# BikeMate Repository Cleanup Report

Date: 2026-07-03

## Inventory Summary

BikeMate contains these active projects:

- `BikeMate.Mobile` - .NET MAUI Android customer/mechanic mobile app.
- `BIKEMATES_ADMIN` - .NET MAUI Android shop-admin/admin app.
- `BikeMate.Api` - ASP.NET Core API, SignalR hubs, auth, file uploads, payments, and service endpoints.
- `BikeMate.Core` - shared DTOs, entities, and constants.
- `BikeMate.Infrastructure` - EF Core SQL Server context, migrations, and database scripts.
- `BikeMate.WebAdmin/BikeMate.WebAdmin` - Blazor web admin app.
- `BikeMate.Tests` - automated test project.
- `tools` - demo/deployment helper scripts.

Largest generated or non-source areas found before cleanup:

- `artifacts` - approximately 896 MB of generated submission packages, documents, ZIPs, APK copies, and temporary slide/doc build files.
- `BikeMate.Mobile/bin` and `BikeMate.Mobile/obj` - approximately 906 MB combined generated build output.
- `BIKEMATES_ADMIN/bin` and `BIKEMATES_ADMIN/obj` - approximately 431 MB combined generated build output.
- `BikeMate.Api/bin` and `BikeMate.Api/obj` - approximately 114 MB combined generated build output.
- `BikeMate.WebAdmin/BikeMate.WebAdmin/bin` and `BikeMate.WebAdmin/BikeMate.WebAdmin/obj` - approximately 92 MB combined generated build output.
- `BikeMate.Api/wwwroot/uploads` - approximately 334 MB of local runtime upload media.
- `.vs` - approximately 7 MB of Visual Studio cache/state.

## Safe To Delete

These items are generated, local-only, duplicated, or replaceable and were approved for cleanup:

- All `bin/` and `obj/` folders.
- `.vs/`.
- `TestResults/` folders if present.
- `publish/` folders if present.
- `node_modules/` folders inside generated artifacts.
- `*.user`, `*.suo`, `*.log`, `*.tmp`, `*.bak`, `*.zip`, `*.apk`, and `*.aab` generated files.
- `artifacts/`, including old PowerPoint, Word, PDF, ZIP, source-package, temporary-render, and phone-demo APK outputs.
- `BikeMate.Api/artifacts/` if present.
- `BikeMate.Api/wwwroot/uploads/*`, except `BikeMate.Api/wwwroot/uploads/.gitkeep`.
- Root-level generated demo screenshots named `bikemate-*.png`.
- Redundant root documentation/report markdown files after consolidating important setup instructions into `README.md`.

## Probably Safe But Needs Review

These were not automatically removed because they may be needed for local provider integration or a developer's machine-specific setup:

- `BikeMate.Api/appsettings.Local.json`
- `BikeMate.WebAdmin/BikeMate.WebAdmin/appsettings.Local.json`
- `BikeMate.Api/firebase-service-account.local.json`
- `BikeMate.Mobile/Platforms/Android/google-services.json`

These files should stay ignored by Git and should not contain committed production secrets.

## Must Keep

These are required for build, runtime behavior, database setup, or phone demo deployment:

- All `.csproj`, `.sln`, and `.slnx` files.
- `BikeMate.Mobile` source, XAML, `App.xaml`, `AppShell.xaml`, `MauiProgram.cs`, `Resources`, `Platforms`, and `Properties`.
- `BIKEMATES_ADMIN` source, XAML, `App.xaml`, `AppShell.xaml`, `MauiProgram.cs`, `Resources`, `Platforms`, `Properties`, and packaged `appsettings.json`.
- `BikeMate.Api` controllers, services, DTOs/models, hubs, `Program.cs`, tracked `appsettings.json`, `appsettings.Development.json`, and `Properties`.
- `BikeMate.Infrastructure` EF Core migrations and SQL scripts.
- `BikeMate.Core` shared code.
- `BikeMate.WebAdmin/BikeMate.WebAdmin` Blazor app source and tracked appsettings.
- `BikeMate.Tests`.
- `tools/Prepare-DemoDatabase.ps1`.
- `tools/Deploy-PhoneDemo.ps1`.
- `tools/Build-PhoneDemoApks.ps1`.
- `tools/Reset-DevData.sql` and `tools/Run-WebAdminLocal.ps1`.
- `.env.example`, appsettings examples, `.gitattributes`, `.gitignore`, and this cleanup report.

## Configuration Check

The Android phone-demo API defaults were verified:

- `BikeMate.Mobile/Helpers/ApiConfig.cs` defaults to `https://bikemate-api-demo.azurewebsites.net/api/`.
- `BIKEMATES_ADMIN/appsettings.json` contains `BaseUrl` and `AndroidBaseUrl` set to `https://bikemate-api-demo.azurewebsites.net/api/`.
- `BIKEMATES_ADMIN/Services/BikeMateDatabaseService.cs` falls back to `https://bikemate-api-demo.azurewebsites.net/api/` for Android.

## Cleanup Decision

The cleanup removes generated outputs and redundant documentation while preserving application behavior. Phone demo scripts remain intact; old APK outputs are removed because they can be regenerated with `tools/Build-PhoneDemoApks.ps1`.

## Post-Cleanup Verification

The cleanup was verified with:

```powershell
dotnet restore .\BikeMate.sln
dotnet build .\BikeMate.sln --no-restore
dotnet test .\BikeMate.Tests\BikeMate.Tests.csproj --no-restore
```

Results:

- Restore completed without warnings after overriding the vulnerable transitive `Microsoft.OpenApi 2.0.0` package with `Microsoft.OpenApi 2.9.0`.
- Full solution build succeeded with `0 Warning(s), 0 Error(s)`.
- Test suite passed with `106` tests passed, `0` failed, `0` skipped.
- Final generated `bin/`, `obj/`, `TestResults/`, `publish/`, and `node_modules/` outputs were removed again after verification.
