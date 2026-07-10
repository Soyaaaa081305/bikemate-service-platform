# BikeMate

BikeMate is a full-stack motorcycle service platform built with .NET MAUI, ASP.NET Core, Blazor, EF Core, SQL Server, and SignalR. It supports customer service booking, mechanic/rider workflows, shop-admin operations, system-admin review tools, messaging, payments, emergency support, upload handling, and phone-demo deployment.

## Projects

- `BikeMate.Mobile` - Android-first .NET MAUI app for customer and mechanic/rider flows.
- `BIKEMATES_ADMIN` - Android-first .NET MAUI app for shop-admin/admin flows.
- `BikeMate.Api` - ASP.NET Core API with auth, role APIs, SignalR hubs, uploads, payments, reports, and notifications.
- `BikeMate.Core` - shared DTOs, entities, constants, and contracts.
- `BikeMate.Infrastructure` - EF Core SQL Server context, migrations, and database scripts.
- `BikeMate.WebAdmin/BikeMate.WebAdmin` - Blazor web admin portal.
- `BikeMate.Tests` - automated test project.
- `tools` - local/demo utility scripts.

## Requirements

- .NET SDK 10.0 or newer.
- .NET MAUI workload and Android SDK tooling.
- Visual Studio 2026 or a compatible .NET MAUI development environment.
- SQL Server Express, Developer Edition, Azure SQL, or another reachable SQL Server instance.
- Android emulator or physical Android device.
- Optional provider keys for Google OAuth/Maps, SendGrid, PayMongo, Firebase, Agora, and storage.

## Restore And Build

```powershell
dotnet tool restore
dotnet restore .\BikeMate.sln
dotnet build .\BikeMate.sln --no-restore
dotnet test .\BikeMate.Tests\BikeMate.Tests.csproj --no-restore
```

Build the mobile apps directly when preparing Android packages:

```powershell
dotnet build .\BikeMate.Mobile\BikeMate.Mobile.csproj -f net10.0-android
dotnet build .\BIKEMATES_ADMIN\BIKEMATES_ADMIN.csproj -f net10.0-android
```

## Database

Default local development connection:

```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=BikeMatesDB_Dev;Trusted_Connection=True;TrustServerCertificate=True;"
```

Apply migrations:

```powershell
dotnet tool run dotnet-ef database update --project .\BikeMate.Infrastructure\BikeMate.Infrastructure.csproj --startup-project .\BikeMate.Api\BikeMate.Api.csproj
```

Apply SQL Server triggers after the database is created:

```powershell
sqlcmd -S localhost\SQLEXPRESS -d BikeMatesDB_Dev -E -b -i .\BikeMate.Infrastructure\Scripts\sql-server-triggers.sql
```

Important database files:

- `BikeMate.Infrastructure/Migrations`
- `BikeMate.Infrastructure/Scripts/BikeMate_InitialSchema.sql`
- `BikeMate.Infrastructure/Scripts/BikeMate_RunThis_DatabaseSetup.sql`
- `BikeMate.Infrastructure/Scripts/sql-server-triggers.sql`
- `tools/Reset-DevData.sql`

## Run Locally

Run the API:

```powershell
dotnet run --project .\BikeMate.Api\BikeMate.Api.csproj --launch-profile https
```

Run WebAdmin:

```powershell
dotnet run --project .\BikeMate.WebAdmin\BikeMate.WebAdmin\BikeMate.WebAdmin.csproj
```

API URLs:

- `https://localhost:5001`
- `http://localhost:5000`
- Health check: `GET /api/health`
- SignalR hubs: `/hubs/booking`, `/hubs/chat`, `/hubs/emergency`, `/hubs/location`, `/hubs/notification`

## Phone Demo

The Android demo defaults are local again. For a physical phone, run the local API through ngrok and build the APK with the tunnel URL:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\Run-LocalNgrokDemo.ps1 -App Mobile
```

That helper starts `BikeMate.Api` on `http://localhost:5000`, starts or reuses an ngrok tunnel, builds the selected Android app with the tunnel URL, installs it, and launches it.

If ngrok is already running, you can point the Android runner at it directly:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\Run-AndroidDemo.ps1 -App Mobile -UseNgrok
```

Required scripts are kept in `tools`:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\Prepare-DemoDatabase.ps1 -SqlServer "<server>.database.windows.net" -Database "BikeMatesDB_Demo" -SqlUser "<sql-user>" -SqlPassword "<sql-password>"

powershell -ExecutionPolicy Bypass -File .\tools\Deploy-PhoneDemo.ps1 -SqlServer "<server>.database.windows.net" -SqlDatabase "BikeMatesDB_Demo" -SqlUser "<sql-user>" -SqlPassword "<sql-password>"

powershell -ExecutionPolicy Bypass -File .\tools\Build-PhoneDemoApks.ps1

powershell -ExecutionPolicy Bypass -File .\tools\Run-AndroidDemo.ps1 -App Mobile

powershell -ExecutionPolicy Bypass -File .\tools\Test-SecretHygiene.ps1 -IncludeUntracked

powershell -ExecutionPolicy Bypass -File .\tools\Test-CloudDeployment.ps1
```

To store uploads in Cloudinary instead of the App Service filesystem, pass Cloudinary credentials when deploying:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\Deploy-PhoneDemo.ps1 `
  -SqlServer "<server>.database.windows.net" `
  -SqlDatabase "BikeMatesDB_Demo" `
  -SqlUser "<sql-user>" `
  -SqlPassword "<sql-password>" `
  -CloudinaryCloudName "<cloud-name>" `
  -CloudinaryApiKey "<api-key>" `
  -CloudinaryApiSecret "<api-secret>"
```

Clean old app data on test phones before a demo so previous API URL overrides do not survive.

## Operations Quick Sheet

Use these commands from the repository root:

```powershell
cd C:\Users\Admin\Documents\PROJECTSSS\BikeMate
```

### Switch To A New Azure Student Account

Use this when the old Azure account is low on credit. First stop the old cloud apps:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Stop-BikeMateAzureDemo.ps1
```

Log out, then sign in with the new Azure account in the browser/MFA prompt:

```powershell
az logout
az login
az account show -o table
```

Create the new low-cost BikeMate cloud stack. Use a short unique suffix because Azure Web App names are global:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Provision-NewAzureDemo.ps1 `
  -NameSuffix "afaolandez" `
  -BudgetAmount 15 `
  -BuildApks
```

That script creates:

- Free F1 API and WebAdmin apps.
- One SQL Serverless database only.
- SQL auto-pause after 15 minutes.
- A monthly budget guard.
- APKs compiled against the new API URL.

If you already know the provider keys, pass them as parameters instead of editing tracked files:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Provision-NewAzureDemo.ps1 `
  -NameSuffix "afaolandez" `
  -BudgetAmount 15 `
  -SendGridApiKey "<sendgrid-api-key>" `
  -SendGridFromEmail "<verified-sender-email>" `
  -GoogleMapsApiKey "<google-maps-api-key>" `
  -GoogleWebClientId "<google-web-client-id>" `
  -GoogleAndroidClientId "<google-android-client-id>" `
  -GoogleWebClientSecret "<google-web-client-secret>" `
  -PayMongoPublicKey "<paymongo-public-key>" `
  -PayMongoSecretKey "<paymongo-secret-key>" `
  -PayMongoWebhookSecret "<paymongo-webhook-secret>" `
  -CloudinaryCloudName "<cloudinary-cloud-name>" `
  -CloudinaryApiKey "<cloudinary-api-key>" `
  -CloudinaryApiSecret "<cloudinary-api-secret>" `
  -AgoraAppId "<agora-app-id>" `
  -AgoraPrimaryCertificate "<agora-primary-certificate>" `
  -BuildApks
```

If you rebuild APKs later for any cloud URL:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Build-PhoneDemoApks.ps1 `
  -ApiBaseUrl "https://your-ngrok-domain.ngrok-free.app/api/"
```

### Turn The Cloud Demo On Or Off

Start the Azure API and WebAdmin only when you are testing or demoing:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Start-BikeMateAzureDemo.ps1
```

Verify the deployed API/admin setup:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Test-CloudDeployment.ps1
```

Stop the cloud apps again to reduce spend:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Stop-BikeMateAzureDemo.ps1
```

Check current Azure state:

```powershell
az webapp list --resource-group BikeMateDemoRG --query "[].{name:name,state:state,host:defaultHostName}" -o table
az sql db show --resource-group BikeMateDemoRG1 --server bikemate-demo-sql-noda --name BikeMatesDB_Demo --query "{name:name,status:status,sku:sku.name,autoPauseDelay:autoPauseDelay}" -o table
```

### Build APKs

Build fresh APKs:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Build-PhoneDemoApks.ps1 -Configuration Debug
```

Fresh APK output:

- `artifacts\phone-demo\apk\BikeMate.Mobile.Debug.apk`
- `artifacts\phone-demo\apk\BIKEMATES_ADMIN.Debug.apk`

The build script also refreshes install-friendly copies here:

- `artifacts\BikeMate.apk`
- `artifacts\BikeMate.Shop.apk`

The Android package IDs are:

- Customer/mechanic app: `com.bikemate.mobile`
- Shop/admin app: `com.bikemate.shop`

### Install, Clear, Launch, Or Uninstall On Android

If `adb` is not on PATH, use the SDK path directly:

```powershell
$adb = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
& $adb devices
```

Install the stable copied APKs:

```powershell
& $adb install -r ".\artifacts\BikeMate.apk"
& $adb install -r ".\artifacts\BikeMate.Shop.apk"
```

Install fresh APKs from the build script:

```powershell
& $adb install -r ".\artifacts\phone-demo\apk\BikeMate.Mobile.Debug.apk"
& $adb install -r ".\artifacts\phone-demo\apk\BIKEMATES_ADMIN.Debug.apk"
```

Clear app data without uninstalling:

```powershell
& $adb shell pm clear com.bikemate.mobile
& $adb shell pm clear com.bikemate.shop
```

Launch the apps:

```powershell
& $adb shell monkey -p com.bikemate.mobile 1
& $adb shell monkey -p com.bikemate.shop 1
```

Uninstall both apps:

```powershell
& $adb uninstall com.bikemate.mobile
& $adb uninstall com.bikemate.shop
```

Remove the old shop/admin package if it was installed from an earlier build:

```powershell
& $adb uninstall com.companyname.bikemates_admin
```

Build, install, and launch through the helper script:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Run-AndroidDemo.ps1 -App Both
```

### Updating Rotated Credentials

Never paste real keys into tracked files. Put rotated credentials in Azure App Settings, ignored local config, provider dashboards, or environment variables.

Update API app settings in Azure:

```powershell
az webapp config appsettings set `
  --resource-group BikeMateDemoRG `
  --name bikemate-api-afaolandez `
  --settings `
    "GoogleMaps__ApiKey=<google-maps-api-key>" `
    "GoogleAuth__WebClientId=<google-web-client-id>" `
    "GoogleAuth__WebClientSecret=<google-web-client-secret>" `
    "GoogleAuth__AndroidClientId=<google-android-client-id>" `
    "SendGrid__ApiKey=<sendgrid-api-key>" `
    "SendGrid__FromEmail=<verified-sender-email>" `
    "PayMongo__PublicKey=<paymongo-public-key>" `
    "PayMongo__SecretKey=<paymongo-secret-key>" `
    "PayMongo__WebhookSecret=<paymongo-webhook-secret>" `
    "Cloudinary__CloudName=<cloudinary-cloud-name>" `
    "Cloudinary__ApiKey=<cloudinary-api-key>" `
    "Cloudinary__ApiSecret=<cloudinary-api-secret>" `
    "Agora__AppId=<agora-app-id>" `
    "Agora__PrimaryCertificate=<agora-primary-certificate>"
```

Update WebAdmin email and Agora settings:

```powershell
az webapp config appsettings set `
  --resource-group BikeMateDemoRG `
  --name bikemate-admin-afaolandez `
  --settings `
    "SendGrid__ApiKey=<sendgrid-api-key>" `
    "SendGrid__FromEmail=<verified-sender-email>" `
    "SendGrid__FromName=BikeMate" `
    "Agora__AppId=<agora-app-id>" `
    "Agora__PrimaryCertificate=<agora-primary-certificate>"
```

After changing credentials, restart and smoke-test:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Start-BikeMateAzureDemo.ps1
az webapp restart --resource-group BikeMateDemoRG --name bikemate-api-afaolandez
az webapp restart --resource-group BikeMateDemoRG --name bikemate-admin-afaolandez
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Test-CloudDeployment.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Stop-BikeMateAzureDemo.ps1
```

Check that SendGrid is configured in Azure without printing secret values:

```powershell
$apps = @("bikemate-api-afaolandez", "bikemate-admin-afaolandez")
foreach ($app in $apps) {
  $settings = az webapp config appsettings list --resource-group BikeMateDemoRG --name $app | ConvertFrom-Json
  Write-Host "[$app]"
  foreach ($name in @("SendGrid__ApiKey", "SendGrid__FromEmail", "SendGrid__FromName")) {
    $item = $settings | Where-Object { $_.name -eq $name } | Select-Object -First 1
    $configured = $null -ne $item -and -not [string]::IsNullOrWhiteSpace($item.value) -and -not $item.value.StartsWith("YOUR_", [StringComparison]::OrdinalIgnoreCase)
    Write-Host "$name configured: $configured"
  }
}
```

Admin website OTP is sent to the email typed at login, as long as that account is active and has the `SystemAdmin` role. Admin accounts created from the Admin Accounts page are active by default and can receive a fresh login code from the same page.

### Before Pushing To GitHub

Run the full local verification pass:

```powershell
dotnet build .\BikeMate.WebAdmin\BikeMate.WebAdmin\BikeMate.WebAdmin.csproj
dotnet build .\BikeMate.Mobile\BikeMate.Mobile.csproj -f net10.0-android
dotnet build .\BIKEMATES_ADMIN\BIKEMATES_ADMIN.csproj -f net10.0-android
dotnet test .\BikeMate.Tests\BikeMate.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Test-SecretHygiene.ps1 -IncludeUntracked
git diff --check
```

Check that ignored local secret files are not tracked:

```powershell
git check-ignore -v .env BikeMate.Api/appsettings.Local.json BikeMate.WebAdmin/BikeMate.WebAdmin/appsettings.Local.json BikeMate.Mobile/Platforms/Android/google-services.json artifacts/BikeMate.apk artifacts/BikeMate.Shop.apk
git ls-files .env BikeMate.Api/appsettings.Local.json BikeMate.WebAdmin/BikeMate.WebAdmin/appsettings.Local.json BikeMate.Mobile/Platforms/Android/google-services.json artifacts/BikeMate.apk artifacts/BikeMate.Shop.apk
```

The second command should print nothing.

## Test Accounts

| Role | Email | Password |
| --- | --- | --- |
| Customer | `customer1@bikemate.test` | `Demo123!` |
| Mechanic | `mechanic1@bikemate.test` | `Demo123!` |
| ShopAdmin | `shop1@bikemate.test` | `Demo123!` |
| SystemAdmin | `isaiahandreinoda@gmail.com` | `Demo123!` |

## Configuration And Secrets

Do not commit real secrets. Use ignored local files, user secrets, environment variables, or provider dashboards for:

- JWT signing key.
- Google OAuth and Google Maps keys.
- SendGrid or SMTP credentials.
- PayMongo public key, secret key, and webhook secret.
- Firebase service-account JSON and Android `google-services.json`.
- Agora credentials.
- Storage provider credentials.

Ignored local config files include:

- `BikeMate.Api/appsettings.Local.json`
- `BikeMate.WebAdmin/BikeMate.WebAdmin/appsettings.Local.json`
- `BikeMate.Api/firebase-service-account*.json`
- `BikeMate.Mobile/Platforms/Android/google-services.json`

If GitHub reports a leaked Google API key, create a new restricted key in
Google Cloud Console, update Azure or your ignored local config with the new
value, then delete the leaked key. Do not reuse a key after it has appeared in a
public commit or generated artifact.

## Repository Hygiene

Generated build outputs and local runtime files are intentionally ignored:

- `bin/`, `obj/`, `.vs/`, `TestResults/`, `publish/`, `artifacts/`, `node_modules/`
- `*.log`, `*.tmp`, `*.bak`, `*.zip`, `*.apk`, `*.aab`, `*.user`, `*.suo`
- `BikeMate.Api/wwwroot/uploads/*` except `.gitkeep`

See `CLEANUP_REPORT.md` for the cleanup classification and decisions.
