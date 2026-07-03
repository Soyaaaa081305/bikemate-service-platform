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
    [string]$AgoraAppId = "fa02725c96dd450ea81fcc2bb5b0e3f7",
    [string]$AgoraPrimaryCertificate = "033f1344bd5c493d91d5e600a7b60c2a",
    [string]$AgoraSecondaryCertificate = "547dc1e13b2f45d48507ad0ec44b91af",
    [string]$GoogleMapsApiKey = "",
    [string]$GoogleWebClientId = "",
    [string]$GoogleAndroidClientId = "",
    [string]$GoogleWebClientSecret = "",
    [string]$SendGridApiKey = "",
    [string]$SendGridFromEmail = "no-reply@bikemate.local",
    [string]$PayMongoPublicKey = "",
    [string]$PayMongoSecretKey = "",
    [string]$PayMongoWebhookSecret = ""
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Azure CLI is not installed. Install it, run 'az login', then rerun this script."
}

az account show --only-show-errors | Out-Null

if ([string]::IsNullOrWhiteSpace($JwtKey)) {
    $bytes = New-Object byte[] 48
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
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

function Ensure-AppServicePlan {
    param(
        [string]$Name,
        [string]$Group,
        [string]$Region
    )

    $existing = az appservice plan show --name $Name --resource-group $Group --only-show-errors 2>$null
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

    $existing = az webapp show --resource-group $Group --name $Name --only-show-errors 2>$null
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($existing)) {
        Write-Host "Using existing Web App: $Name"
        return
    }

    az webapp create --resource-group $Group --plan $Plan --name $Name --only-show-errors | Out-Null
}

New-Item -ItemType Directory -Force -Path $artifacts | Out-Null
Remove-Item -Recurse -Force $apiPublish, $webPublish -ErrorAction SilentlyContinue
Remove-Item -Force $apiZip, $webZip -ErrorAction SilentlyContinue

Push-Location $repoRoot
try {
    Write-Host "Publishing API and WebAdmin as self-contained win-x64 apps..."
    dotnet publish .\BikeMate.Api\BikeMate.Api.csproj -c Release -r win-x64 --self-contained true -o $apiPublish
    dotnet publish .\BikeMate.WebAdmin\BikeMate.WebAdmin\BikeMate.WebAdmin.csproj -c Release -r win-x64 --self-contained true -o $webPublish
    Compress-Archive -Path (Join-Path $apiPublish "*") -DestinationPath $apiZip -Force
    Compress-Archive -Path (Join-Path $webPublish "*") -DestinationPath $webZip -Force

    Write-Host "Creating Azure resource group and Free F1 App Service plan..."
    az group create --name $ResourceGroup --location $Location --only-show-errors | Out-Null
    Ensure-AppServicePlan -Name $PlanName -Group $ResourceGroup -Region $Location

    Write-Host "Creating web apps if needed..."
    Ensure-WebApp -Name $ApiAppName -Group $ResourceGroup -Plan $PlanName
    Ensure-WebApp -Name $WebAdminAppName -Group $ResourceGroup -Plan $PlanName

    $apiSettings = @(
        "ASPNETCORE_ENVIRONMENT=Production",
        "ConnectionStrings__DefaultConnection=$sqlConnectionString",
        "Jwt__Issuer=BikeMate",
        "Jwt__Audience=BikeMateMobile",
        "Jwt__Key=$JwtKey",
        "Cors__AllowedOrigins__0=$webUrl",
        "GoogleMaps__ApiKey=$GoogleMapsApiKey",
        "GoogleAuth__ClientId=$GoogleWebClientId",
        "GoogleAuth__WebClientId=$GoogleWebClientId",
        "GoogleAuth__AndroidClientId=$GoogleAndroidClientId",
        "GoogleAuth__WebClientSecret=$GoogleWebClientSecret",
        "GoogleAuth__RedirectUri=$apiUrl/api/auth/google/callback",
        "GoogleAuth__MobileCallbackUri=bikemate://auth/google",
        "GoogleAuth__ClientIds__0=$GoogleAndroidClientId",
        "GoogleAuth__ClientIds__1=$GoogleWebClientId",
        "SendGrid__ApiKey=$SendGridApiKey",
        "SendGrid__FromEmail=$SendGridFromEmail",
        "SendGrid__FromName=BikeMate",
        "PayMongo__PublicKey=$PayMongoPublicKey",
        "PayMongo__SecretKey=$PayMongoSecretKey",
        "PayMongo__WebhookSecret=$PayMongoWebhookSecret",
        "PayMongo__SuccessUrl=bikemate://payment-success",
        "PayMongo__CancelUrl=bikemate://payment-cancelled",
        "Agora__AppId=$AgoraAppId",
        "Agora__PrimaryCertificate=$AgoraPrimaryCertificate",
        "Agora__SecondaryCertificate=$AgoraSecondaryCertificate",
        "Agora__TokenLifetimeSeconds=1800",
        "Storage__Provider=Local",
        "Storage__Mode=Local",
        "Storage__BaseUrl=$apiUrl/uploads",
        "Storage__MaxFileBytes=52428800"
    )

    $webSettings = @(
        "ASPNETCORE_ENVIRONMENT=Production",
        "ConnectionStrings__BikeMateDb=$sqlConnectionString",
        "Api__PublicBaseUrl=$apiUrl",
        "Agora__AppId=$AgoraAppId",
        "Agora__PrimaryCertificate=$AgoraPrimaryCertificate",
        "Agora__SecondaryCertificate=$AgoraSecondaryCertificate",
        "Agora__TokenLifetimeSeconds=1800"
    )

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
