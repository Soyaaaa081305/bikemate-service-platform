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

The Android demo defaults are configured for:

```text
https://bikemate-api-demo.azurewebsites.net/api/
```

Required scripts are kept in `tools`:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\Prepare-DemoDatabase.ps1 -SqlServer "<server>.database.windows.net" -Database "BikeMatesDB_Demo" -SqlUser "<sql-user>" -SqlPassword "<sql-password>"

powershell -ExecutionPolicy Bypass -File .\tools\Deploy-PhoneDemo.ps1 -SqlServer "<server>.database.windows.net" -SqlDatabase "BikeMatesDB_Demo" -SqlUser "<sql-user>" -SqlPassword "<sql-password>"

powershell -ExecutionPolicy Bypass -File .\tools\Build-PhoneDemoApks.ps1
```

Clean old app data on test phones before a demo so previous API URL overrides do not survive.

## Test Accounts

| Role | Email | Password |
| --- | --- | --- |
| Customer | `customer@bikemate.test` | `Password123!` |
| Mechanic | `mechanic@bikemate.test` | `Password123!` |
| ShopAdmin | `shop@bikemate.test` | `Password123!` |
| SystemAdmin | `admin@bikemate.test` | `Password123!` |

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

## Repository Hygiene

Generated build outputs and local runtime files are intentionally ignored:

- `bin/`, `obj/`, `.vs/`, `TestResults/`, `publish/`, `artifacts/`, `node_modules/`
- `*.log`, `*.tmp`, `*.bak`, `*.zip`, `*.apk`, `*.aab`, `*.user`, `*.suo`
- `BikeMate.Api/wwwroot/uploads/*` except `.gitkeep`

See `CLEANUP_REPORT.md` for the cleanup classification and decisions.
