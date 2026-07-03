# Project Inspection Report

Date inspected: 2026-06-09

## Solution Files

The project contains both:

- `BikeMate.sln`
- `BikeMate.slnx`

Both include the same four projects. Use `BikeMate.slnx` for command-line restore/build, or `BikeMate.sln` in Visual Studio.

## Projects Found

| Project | Purpose | Target framework |
| --- | --- | --- |
| `BikeMate.Mobile` | .NET MAUI Android app | `net10.0-android` |
| `BikeMate.Api` | ASP.NET Core Web API | `net10.0` |
| `BikeMate.Core` | Shared constants, DTOs, entities | `net10.0` |
| `BikeMate.Infrastructure` | EF Core SQL Server DbContext and migrations | `net10.0` |

No test project is currently present.

## NuGet Packages Found

Mobile:

- `CommunityToolkit.Mvvm`
- `Microsoft.AspNetCore.SignalR.Client`
- `Microsoft.Extensions.Http`
- `Microsoft.Maui.Controls`
- `Microsoft.Extensions.Logging.Debug`

API:

- `BCrypt.Net-Next`
- `Google.Apis.Auth`
- `MailKit`
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Microsoft.AspNetCore.OpenApi`
- `Microsoft.EntityFrameworkCore.Design`
- `System.IdentityModel.Tokens.Jwt`

Infrastructure:

- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.EntityFrameworkCore.Tools`
- `Microsoft.EntityFrameworkCore.Design`

## Database Files

EF Core migration exists:

- `BikeMate.Infrastructure\Migrations\20260608143854_InitialBikeMateSchema.cs`

SQL scripts exist:

- `BikeMate.Infrastructure\Scripts\BikeMate_InitialSchema.sql`
- `BikeMate.Infrastructure\Scripts\sql-server-triggers.sql`

## Appsettings Files

Found:

- `BikeMate.Api\appsettings.json`
- `BikeMate.Api\appsettings.Development.json`

Development connection string:

```text
Server=localhost\SQLEXPRESS;Database=BikeMatesDB_Dev;Trusted_Connection=True;TrustServerCertificate=True;
```

## Mobile Pages and Shells

Found:

- Onboarding/login flow in `MainPage.xaml`
- Auth pages: Login, Register, OTP Verification
- Customer shell
- Mechanic shell
- Shop Admin shell
- System Admin shell
- Prototype pages for Customer, Mechanic, Admin, and Shop Admin modules

## API Controllers

Found:

- Auth
- Customers
- Mechanics
- Shops
- Services
- Products
- Service Requests
- Payments
- Messages/Conversations
- Location
- Devices
- Notifications
- Files
- Admin
- Reports
- Users
- Health

## What Was Complete

- .NET 10 project structure
- MAUI Android target
- ASP.NET Core API
- EF Core SQL Server DbContext and migration
- Seed roles, users, services, shop, mechanic, requests, messages, payments
- JWT login structure
- SignalR chat hub
- PayMongo placeholder structure
- OTP/email placeholder structure
- Google login placeholder structure
- File upload placeholder structure
- Module shells for all four modes

## What Was Broken

- App was launching straight to `LoginPage`, so the original onboarding/summary in `MainPage` was hidden.
- Role-protected endpoints returned `403 Forbidden` after login because JWT role claims were being remapped.
- `GET /api/reports/top-services` returned `400 Bad Request` because EF Core could not translate the LINQ query.
- Mobile API config used HTTP even though the API redirects authenticated calls to HTTPS.
- Beginner setup/report files were missing.

## What Was Fixed

- `AppShell.xaml` now launches `MainPage`.
- `MainPage` now has visible `Skip` and demo mode buttons.
- `ApiConfig` now uses HTTPS URLs and a debug-only local certificate handler.
- JWT role mapping was fixed with `options.MapInboundClaims = false`.
- Top services report query was rewritten into a SQL-translatable query.
- Beginner run guide and final test report were created.

## What Still Needs Real Keys

- PayMongo keys
- Google OAuth client ID
- SMTP email credentials
- Firebase credentials

The app runs locally without those keys using placeholders and development fallbacks.
