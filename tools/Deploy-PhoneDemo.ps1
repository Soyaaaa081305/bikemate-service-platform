param(
    [string]$ResourceGroup = "rg-bikemate-demo",
    [string]$Location = "southeastasia",
    [string]$PlanName = "bikemate-demo-f1-plan",
    [string]$ApiAppName = "bikemate-api-demo",
    [string]$WebAdminAppName = "bikemate-webadmin-demo",

    [Parameter(Mandatory = $true)]
    [string]$SqlServer,

    [Parameter(Mandatory = $true)]
    [string]$SqlDatabase = "BikeMatesDB_Demo",

    [Parameter(Mandatory = $true)]
    [string]$SqlUser,

    [Parameter(Mandatory = $true)]
    [string]$SqlPassword,

    [string]$JwtKey,
    [string]$AgoraAppId = "",
    [string]$AgoraPrimaryCertificate = "",
    [string]$AgoraSecondaryCertificate = "",
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
    [string]$CloudinaryFolder = "bikemate",
    [ValidateSet("", "Local", "Cloudinary")]
    [string]$StorageProvider = "",
    [long]$StorageMaxFileBytes = 26214400
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    $azureCliCandidates = @(
        (Join-Path $env:ProgramFiles "Microsoft SDKs\Azure\CLI2\wbin\az.cmd"),
        (Join-Path ${env:ProgramFiles(x86)} "Microsoft SDKs\Azure\CLI2\wbin\az.cmd")
    )
    $azureCliPath = $azureCliCandidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
    if ($azureCliPath) {
        $env:PATH = "$(Split-Path -Parent $azureCliPath);$env:PATH"
    }
    else {
        throw "Azure CLI is not installed. Install it, run 'az login', then rerun this script."
    }
}

az account show --only-show-errors | Out-Null

if ([string]::IsNullOrWhiteSpace($JwtKey)) {
    $bytes = New-Object byte[] 48
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $rng.GetBytes($bytes)
    }
    finally {
        $rng.Dispose()
    }
    $JwtKey = [Convert]::ToBase64String($bytes)
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $repoRoot "artifacts\phone-demo"
$apiPublish = Join-Path $artifacts "api-publish"
$webPublish = Join-Path $artifacts "webadmin-publish"
$apiZip = Join-Path $artifacts "bikemate-api.zip"
$webZip = Join-Path $artifacts "bikemate-webadmin.zip"
$apiUrl = "https://$ApiAppName.azurewebsites.net"
$webUrl = "https://$WebAdminAppName.azurewebsites.net"
$sqlConnectionString = "Server=tcp:$SqlServer,1433;Initial Catalog=$SqlDatabase;Persist Security Info=False;User ID=$SqlUser;Password=$SqlPassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
$resolvedStorageProvider = if (-not [string]::IsNullOrWhiteSpace($StorageProvider)) {
    $StorageProvider
}
elseif (
    -not [string]::IsNullOrWhiteSpace($CloudinaryCloudName) -and
    -not [string]::IsNullOrWhiteSpace($CloudinaryApiKey) -and
    -not [string]::IsNullOrWhiteSpace($CloudinaryApiSecret)
) {
    "Cloudinary"
}
else {
    ""
}

function Ensure-AppServicePlan {
    param(
        [string]$Name,
        [string]$Group,
        [string]$Region
    )

    $existing = az appservice plan list --resource-group $Group --query "[?name=='$Name'].name | [0]" --output tsv --only-show-errors
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($existing)) {
        Write-Host "Using existing App Service plan: $Name"
        return
    }

    az appservice plan create --name $Name --resource-group $Group --location $Region --sku F1 --only-show-errors | Out-Null
}

function Ensure-WebApp {
    param(
        [string]$Name,
        [string]$Group,
        [string]$Plan
    )

    $existing = az webapp list --resource-group $Group --query "[?name=='$Name'].name | [0]" --output tsv --only-show-errors
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($existing)) {
        Write-Host "Using existing Web App: $Name"
        return
    }

    az webapp create --resource-group $Group --plan $Plan --name $Name --only-show-errors | Out-Null
}

function Add-SettingIfValue {
    param(
        [System.Collections.Generic.List[string]]$Settings,
        [string]$Name,
        [string]$Value
    )

    if (-not [string]::IsNullOrWhiteSpace($Value)) {
        $Settings.Add("$Name=$Value")
    }
}

New-Item -ItemType Directory -Force -Path $artifacts | Out-Null
Remove-Item -Recurse -Force $apiPublish, $webPublish -ErrorAction SilentlyContinue
Remove-Item -Force $apiZip, $webZip -ErrorAction SilentlyContinue

Push-Location $repoRoot
try {
    Write-Host "Publishing API and WebAdmin as self-contained win-x86 apps..."
    dotnet publish .\BikeMate.Api\BikeMate.Api.csproj -c Release -r win-x86 --self-contained true -p:UseAppHost=true -o $apiPublish
    if ($LASTEXITCODE -ne 0) {
        throw "API publish failed with exit code $LASTEXITCODE."
    }

    dotnet publish .\BikeMate.WebAdmin\BikeMate.WebAdmin\BikeMate.WebAdmin.csproj -c Release -r win-x86 --self-contained true -p:UseAppHost=true -o $webPublish
    if ($LASTEXITCODE -ne 0) {
        throw "WebAdmin publish failed with exit code $LASTEXITCODE."
    }

    Compress-Archive -Path (Join-Path $apiPublish "*") -DestinationPath $apiZip -Force
    Compress-Archive -Path (Join-Path $webPublish "*") -DestinationPath $webZip -Force

    Write-Host "Creating Azure resource group and Free F1 App Service plan..."
    az group create --name $ResourceGroup --location $Location --only-show-errors | Out-Null
    Ensure-AppServicePlan -Name $PlanName -Group $ResourceGroup -Region $Location

    Write-Host "Creating web apps if needed..."
    Ensure-WebApp -Name $ApiAppName -Group $ResourceGroup -Plan $PlanName
    Ensure-WebApp -Name $WebAdminAppName -Group $ResourceGroup -Plan $PlanName

    $apiSettings = [System.Collections.Generic.List[string]]@(
        "ASPNETCORE_ENVIRONMENT=Production",
        "ConnectionStrings__DefaultConnection=$sqlConnectionString",
        "Jwt__Issuer=BikeMate",
        "Jwt__Audience=BikeMateMobile",
        "Jwt__Key=$JwtKey",
        "Cors__AllowedOrigins__0=$webUrl",
        "GoogleAuth__RedirectUri=$apiUrl/api/auth/google/callback",
        "GoogleAuth__MobileCallbackUri=bikemate://auth/google",
        "SendGrid__FromName=BikeMate",
        "PayMongo__SuccessUrl=bikemate://payment-success",
        "PayMongo__CancelUrl=bikemate://payment-cancelled",
        "Agora__TokenLifetimeSeconds=1800",
        "Storage__MaxFileBytes=$StorageMaxFileBytes",
        "Cloudinary__Folder=$CloudinaryFolder"
    )
    Add-SettingIfValue -Settings $apiSettings -Name "Storage__Provider" -Value $resolvedStorageProvider
    Add-SettingIfValue -Settings $apiSettings -Name "Storage__Mode" -Value $resolvedStorageProvider
    if ($resolvedStorageProvider -eq "Local") {
        Add-SettingIfValue -Settings $apiSettings -Name "Storage__BaseUrl" -Value "$apiUrl/uploads"
    }
    Add-SettingIfValue -Settings $apiSettings -Name "GoogleMaps__ApiKey" -Value $GoogleMapsApiKey
    Add-SettingIfValue -Settings $apiSettings -Name "GoogleAuth__ClientId" -Value $GoogleWebClientId
    Add-SettingIfValue -Settings $apiSettings -Name "GoogleAuth__WebClientId" -Value $GoogleWebClientId
    Add-SettingIfValue -Settings $apiSettings -Name "GoogleAuth__AndroidClientId" -Value $GoogleAndroidClientId
    Add-SettingIfValue -Settings $apiSettings -Name "GoogleAuth__WebClientSecret" -Value $GoogleWebClientSecret
    Add-SettingIfValue -Settings $apiSettings -Name "GoogleAuth__ClientIds__0" -Value $GoogleAndroidClientId
    Add-SettingIfValue -Settings $apiSettings -Name "GoogleAuth__ClientIds__1" -Value $GoogleWebClientId
    Add-SettingIfValue -Settings $apiSettings -Name "SendGrid__ApiKey" -Value $SendGridApiKey
    Add-SettingIfValue -Settings $apiSettings -Name "SendGrid__FromEmail" -Value $SendGridFromEmail
    Add-SettingIfValue -Settings $apiSettings -Name "PayMongo__PublicKey" -Value $PayMongoPublicKey
    Add-SettingIfValue -Settings $apiSettings -Name "PayMongo__SecretKey" -Value $PayMongoSecretKey
    Add-SettingIfValue -Settings $apiSettings -Name "PayMongo__WebhookSecret" -Value $PayMongoWebhookSecret
    Add-SettingIfValue -Settings $apiSettings -Name "Agora__AppId" -Value $AgoraAppId
    Add-SettingIfValue -Settings $apiSettings -Name "Agora__PrimaryCertificate" -Value $AgoraPrimaryCertificate
    Add-SettingIfValue -Settings $apiSettings -Name "Agora__SecondaryCertificate" -Value $AgoraSecondaryCertificate
    Add-SettingIfValue -Settings $apiSettings -Name "Cloudinary__CloudName" -Value $CloudinaryCloudName
    Add-SettingIfValue -Settings $apiSettings -Name "Cloudinary__ApiKey" -Value $CloudinaryApiKey
    Add-SettingIfValue -Settings $apiSettings -Name "Cloudinary__ApiSecret" -Value $CloudinaryApiSecret

    $webSettings = [System.Collections.Generic.List[string]]@(
        "ASPNETCORE_ENVIRONMENT=Production",
        "ConnectionStrings__BikeMateDb=$sqlConnectionString",
        "Api__PublicBaseUrl=$apiUrl",
        "SendGrid__FromName=BikeMate",
        "Agora__TokenLifetimeSeconds=1800"
    )
    Add-SettingIfValue -Settings $webSettings -Name "SendGrid__ApiKey" -Value $SendGridApiKey
    Add-SettingIfValue -Settings $webSettings -Name "SendGrid__FromEmail" -Value $SendGridFromEmail
    Add-SettingIfValue -Settings $webSettings -Name "Agora__AppId" -Value $AgoraAppId
    Add-SettingIfValue -Settings $webSettings -Name "Agora__PrimaryCertificate" -Value $AgoraPrimaryCertificate
    Add-SettingIfValue -Settings $webSettings -Name "Agora__SecondaryCertificate" -Value $AgoraSecondaryCertificate

    Write-Host "Configuring app settings..."
    az webapp config appsettings set --resource-group $ResourceGroup --name $ApiAppName --settings $apiSettings --only-show-errors | Out-Null
    az webapp config appsettings set --resource-group $ResourceGroup --name $WebAdminAppName --settings $webSettings --only-show-errors | Out-Null

    Write-Host "Deploying zip packages..."
    az webapp deployment source config-zip --resource-group $ResourceGroup --name $ApiAppName --src $apiZip --only-show-errors | Out-Null
    az webapp deployment source config-zip --resource-group $ResourceGroup --name $WebAdminAppName --src $webZip --only-show-errors | Out-Null

    Write-Host "Restarting apps..."
    az webapp restart --resource-group $ResourceGroup --name $ApiAppName --only-show-errors
    az webapp restart --resource-group $ResourceGroup --name $WebAdminAppName --only-show-errors

    Write-Host "API: $apiUrl"
    Write-Host "WebAdmin: $webUrl"
}
finally {
    Pop-Location
}
