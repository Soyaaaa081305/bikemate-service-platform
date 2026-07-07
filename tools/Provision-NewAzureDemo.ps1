param(
    [string]$ResourceGroup = "BikeMateDemoRG",
    [string]$SqlResourceGroup = "BikeMateDemoRG1",
    [string]$Location = "eastasia",
    [string]$NameSuffix = "",
    [string]$PlanName = "",
    [string]$ApiAppName = "",
    [string]$WebAdminAppName = "",
    [string]$SqlServerName = "",
    [string]$SqlDatabase = "BikeMatesDB_Demo",
    [string]$SqlUser = "bikemateadmin",
    [securestring]$SqlPassword,
    [decimal]$BudgetAmount = 15,
    [string]$BudgetName = "BikeMateStudentCreditGuard",
    [switch]$SkipDatabasePrepare,
    [switch]$BuildApks,

    [string]$GoogleMapsApiKey = "",
    [string]$GoogleWebClientId = "",
    [string]$GoogleAndroidClientId = "",
    [string]$GoogleWebClientSecret = "",
    [string]$SendGridApiKey = "",
    [string]$SendGridFromEmail = "",
    [string]$PayMongoPublicKey = "",
    [string]$PayMongoSecretKey = "",
    [string]$PayMongoWebhookSecret = "",
    [string]$CloudinaryCloudName = "",
    [string]$CloudinaryApiKey = "",
    [string]$CloudinaryApiSecret = "",
    [string]$AgoraAppId = "",
    [string]$AgoraPrimaryCertificate = "",
    [string]$AgoraSecondaryCertificate = ""
)

$ErrorActionPreference = "Stop"

function Convert-SecureStringToPlainText {
    param([securestring]$Value)

    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Value)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

function Sanitize-NamePart {
    param([string]$Value)

    $clean = ($Value.ToLowerInvariant() -replace "[^a-z0-9-]", "")
    $clean = $clean.Trim("-")
    if ([string]::IsNullOrWhiteSpace($clean)) {
        $clean = "demo"
    }

    return $clean
}

function Ensure-Az {
    if (Get-Command az -ErrorAction SilentlyContinue) {
        return
    }

    $candidate = Join-Path $env:ProgramFiles "Microsoft SDKs\Azure\CLI2\wbin\az.cmd"
    if (Test-Path $candidate) {
        $env:PATH = "$(Split-Path -Parent $candidate);$env:PATH"
        return
    }

    throw "Azure CLI was not found. Install Azure CLI, then run az login."
}

function Ensure-ResourceGroup {
    param(
        [string]$Name,
        [string]$Region
    )

    $existingLocation = az group show --name $Name --query "location" -o tsv --only-show-errors 2>$null
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($existingLocation)) {
        Write-Host "Using existing resource group: $Name ($existingLocation)"
        return
    }

    az group create --name $Name --location $Region --only-show-errors | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create resource group $Name in $Region."
    }
}

function Test-SqlServerExists {
    param(
        [string]$Group,
        [string]$Name
    )

    $oldErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        $result = az sql server show --resource-group $Group --name $Name --query "name" -o tsv --only-show-errors 2>$null
        return $LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($result)
    }
    finally {
        $ErrorActionPreference = $oldErrorActionPreference
    }
}

function Test-SqlDatabaseExists {
    param(
        [string]$Group,
        [string]$Server,
        [string]$Name
    )

    $oldErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        $result = az sql db show --resource-group $Group --server $Server --name $Name --query "name" -o tsv --only-show-errors 2>$null
        return $LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($result)
    }
    finally {
        $ErrorActionPreference = $oldErrorActionPreference
    }
}

Ensure-Az

$accountJson = az account show --only-show-errors
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($accountJson)) {
    throw "Azure CLI is not logged in. Run az login with the new account, then rerun this script."
}

$account = $accountJson | ConvertFrom-Json
$accountUser = [string]$account.user.name
if ([string]::IsNullOrWhiteSpace($NameSuffix)) {
    $NameSuffix = ($accountUser -split "@")[0]
}

$suffix = Sanitize-NamePart $NameSuffix
if ($suffix.Length -gt 24) {
    $suffix = $suffix.Substring(0, 24).Trim("-")
}

if ([string]::IsNullOrWhiteSpace($PlanName)) {
    $PlanName = "bikemate-f1-$suffix"
}

if ([string]::IsNullOrWhiteSpace($ApiAppName)) {
    $ApiAppName = "bikemate-api-$suffix"
}

if ([string]::IsNullOrWhiteSpace($WebAdminAppName)) {
    $WebAdminAppName = "bikemate-admin-$suffix"
}

if ([string]::IsNullOrWhiteSpace($SqlServerName)) {
    $SqlServerName = "bikemate-sql-$suffix"
}

if ($ApiAppName.Length -gt 60 -or $WebAdminAppName.Length -gt 60 -or $SqlServerName.Length -gt 63) {
    throw "Generated resource names are too long. Rerun with a shorter -NameSuffix."
}

if ($null -eq $SqlPassword) {
    $SqlPassword = Read-Host "Enter a new SQL admin password for $SqlUser" -AsSecureString
}

$sqlPasswordPlain = Convert-SecureStringToPlainText $SqlPassword
if ([string]::IsNullOrWhiteSpace($sqlPasswordPlain) -or $sqlPasswordPlain.Length -lt 12) {
    throw "SQL password must be at least 12 characters."
}

$apiUrl = "https://$ApiAppName.azurewebsites.net"

Write-Host "Using Azure subscription: $($account.name) / $($account.id)"
Write-Host "API app: $ApiAppName"
Write-Host "WebAdmin app: $WebAdminAppName"
Write-Host "SQL server: $SqlServerName.database.windows.net"
Write-Host "Database: $SqlDatabase"

Write-Host "Creating resource groups..."
Ensure-ResourceGroup -Name $ResourceGroup -Region $Location
Ensure-ResourceGroup -Name $SqlResourceGroup -Region $Location

Write-Host "Creating a monthly budget guard..."
$startDate = Get-Date -Format "yyyy-MM-01"
$endDate = "2026-12-31"
$oldErrorActionPreference = $ErrorActionPreference
try {
    $ErrorActionPreference = "Continue"
    $existingBudget = az consumption budget list --query "[?name=='$BudgetName'].name | [0]" -o tsv --only-show-errors 2>$null
    if ([string]::IsNullOrWhiteSpace($existingBudget)) {
        az consumption budget create `
            --budget-name $BudgetName `
            --category cost `
            --amount $BudgetAmount `
            --start-date $startDate `
            --end-date $endDate `
            --time-grain monthly `
            --only-show-errors 2>$null | Out-Null

        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Azure CLI could not create the budget automatically. Continue deployment, then create a $BudgetAmount USD budget manually in Azure Portal > Cost Management."
        }
    }
    else {
        Write-Host "Budget already exists: $BudgetName"
    }
}
finally {
    $ErrorActionPreference = $oldErrorActionPreference
}

Write-Host "Creating SQL server if needed..."
$serverExists = Test-SqlServerExists -Group $SqlResourceGroup -Name $SqlServerName
if (-not $serverExists) {
    az sql server create `
        --resource-group $SqlResourceGroup `
        --name $SqlServerName `
        --location $Location `
        --admin-user $SqlUser `
        --admin-password $sqlPasswordPlain `
        --only-show-errors | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create SQL server $SqlServerName in $Location. Try a different -Location if Azure policy blocks this region."
    }
}

Write-Host "Configuring SQL firewall rules..."
az sql server firewall-rule create `
    --resource-group $SqlResourceGroup `
    --server $SqlServerName `
    --name AllowAzureServices `
    --start-ip-address 0.0.0.0 `
    --end-ip-address 0.0.0.0 `
    --only-show-errors | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Failed to create SQL firewall rule AllowAzureServices." }

try {
    $clientIp = (Invoke-RestMethod -Uri "https://api.ipify.org" -TimeoutSec 15).Trim()
    if (-not [string]::IsNullOrWhiteSpace($clientIp)) {
        az sql server firewall-rule create `
            --resource-group $SqlResourceGroup `
            --server $SqlServerName `
            --name CurrentDeveloperMachine `
            --start-ip-address $clientIp `
            --end-ip-address $clientIp `
            --only-show-errors | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Could not add your current IP to the SQL firewall. Database prepare may fail until this IP is allowed: $clientIp"
        }
    }
}
catch {
    Write-Warning "Could not detect public IP for SQL firewall. Database prepare may fail until you add your IP in Azure."
}

Write-Host "Creating serverless SQL database if needed..."
$dbExists = Test-SqlDatabaseExists -Group $SqlResourceGroup -Server $SqlServerName -Name $SqlDatabase
if (-not $dbExists) {
    az sql db create `
        --resource-group $SqlResourceGroup `
        --server $SqlServerName `
        --name $SqlDatabase `
        --edition GeneralPurpose `
        --family Gen5 `
        --capacity 1 `
        --compute-model Serverless `
        --min-capacity 0.5 `
        --auto-pause-delay 15 `
        --backup-storage-redundancy Local `
        --only-show-errors | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Failed to create SQL database $SqlDatabase on $SqlServerName." }
}

if (-not $SkipDatabasePrepare) {
    Write-Host "Preparing database schema and demo data..."
    & "$PSScriptRoot\Prepare-DemoDatabase.ps1" `
        -SqlServer "$SqlServerName.database.windows.net" `
        -Database $SqlDatabase `
        -SqlUser $SqlUser `
        -SqlPassword $sqlPasswordPlain
    if ($LASTEXITCODE -ne 0) { throw "Database prepare failed. Fix the SQL error above before deploying apps." }
}

Write-Host "Deploying API and WebAdmin..."
$deployArgs = @{
    ResourceGroup = $ResourceGroup
    Location = $Location
    PlanName = $PlanName
    ApiAppName = $ApiAppName
    WebAdminAppName = $WebAdminAppName
    SqlServer = "$SqlServerName.database.windows.net"
    SqlDatabase = $SqlDatabase
    SqlUser = $SqlUser
    SqlPassword = $sqlPasswordPlain
    GoogleMapsApiKey = $GoogleMapsApiKey
    GoogleWebClientId = $GoogleWebClientId
    GoogleAndroidClientId = $GoogleAndroidClientId
    GoogleWebClientSecret = $GoogleWebClientSecret
    SendGridApiKey = $SendGridApiKey
    SendGridFromEmail = $SendGridFromEmail
    PayMongoPublicKey = $PayMongoPublicKey
    PayMongoSecretKey = $PayMongoSecretKey
    PayMongoWebhookSecret = $PayMongoWebhookSecret
    CloudinaryCloudName = $CloudinaryCloudName
    CloudinaryApiKey = $CloudinaryApiKey
    CloudinaryApiSecret = $CloudinaryApiSecret
    AgoraAppId = $AgoraAppId
    AgoraPrimaryCertificate = $AgoraPrimaryCertificate
    AgoraSecondaryCertificate = $AgoraSecondaryCertificate
}
& "$PSScriptRoot\Deploy-PhoneDemo.ps1" @deployArgs

if ($BuildApks) {
    Write-Host "Building APKs for $apiUrl/api/..."
    & "$PSScriptRoot\Build-PhoneDemoApks.ps1" -Configuration Debug -ApiBaseUrl "$apiUrl/api/"
}

Write-Host "Done."
Write-Host "API: $apiUrl"
Write-Host "WebAdmin: https://$WebAdminAppName.azurewebsites.net"
Write-Host "Build APKs later with:"
Write-Host "powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Build-PhoneDemoApks.ps1 -ApiBaseUrl $apiUrl/api/"
