# BikeMate Setup Guide

BikeMate is an Android-first .NET MAUI app with an ASP.NET Core API, EF Core SQL Server backend, shared DTO/entity project, role-based routing, seeded sample data, SignalR chat support, PayMongo checkout placeholders, and module screens for Customer, Mechanic, System Admin, and Shop Admin workflows.

## Start Here

- Beginner run guide: `RUN_BIKEMATE_STEP_BY_STEP.md`
- Requirements checklist: `BEGINNER_REQUIREMENTS_CHECKLIST.md`
- API testing guide: `API_TESTING_GUIDE.md`
- Final test report: `FINAL_TEST_REPORT.md`
- Project inspection report: `PROJECT_INSPECTION_REPORT.md`

## 1. Requirements

- .NET SDK 10.0 or newer
- .NET MAUI workload
- Visual Studio 2026 or later with Android tooling
- SQL Server 2022, SQL Server Developer Edition, or a reachable SQL Server instance
- Android emulator or Android device
- Optional keys for Google OAuth, SMTP/SendGrid, PayMongo, Firebase Cloud Messaging, and file storage

Restore local tools and packages:

```powershell
dotnet tool restore
dotnet restore .\BikeMate.sln
```

## 2. Database Setup

Default development connection:

```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=BikeMatesDB_Dev;Trusted_Connection=True;TrustServerCertificate=True;"
```

Apply the EF Core migration:

```powershell
dotnet tool run dotnet-ef database update --project .\BikeMate.Infrastructure\BikeMate.Infrastructure.csproj --startup-project .\BikeMate.Api\BikeMate.Api.csproj
```

Apply the SQL Server trigger pack after the migration:

```powershell
sqlcmd -S localhost\SQLEXPRESS -d BikeMatesDB_Dev -E -b -i .\BikeMate.Infrastructure\Scripts\sql-server-triggers.sql
```

Generated SQL script:

```text
BikeMate.Infrastructure\Scripts\BikeMate_InitialSchema.sql
```

Trigger reference script:

```text
BikeMate.Infrastructure\Scripts\sql-server-triggers.sql
```

The migration includes lookup data, seed users, sample customer/mechanic/shop data, services, products, bookings, messages, payment history, notifications, and live location. The separate trigger script adds mechanic rating, completed job count, and status history triggers.

## 3. Backend Setup

Run the API:

```powershell
dotnet run --project .\BikeMate.Api\BikeMate.Api.csproj --launch-profile https
```

API URLs:

- `https://localhost:5001`
- `http://localhost:5000`
- Health check: `GET /api/health`
- SignalR hub: `/hubs/chat`

Main endpoint groups:

- `/api/auth`
- `/api/customers`
- `/api/mechanics`
- `/api/shops`
- `/api/services`
- `/api/products`
- `/api/service-requests`
- `/api/conversations`
- `/api/payments`
- `/api/location`
- `/api/devices`
- `/api/notifications`
- `/api/admin`
- `/api/reports`
- `/api/files`

## 4. MAUI App Setup

Android emulator API base URL:

```csharp
https://10.0.2.2:5001/api
```

Build Android:

```powershell
dotnet build .\BikeMate.Mobile\BikeMate.Mobile.csproj -f net10.0-android
```

Run from Visual Studio using an Android emulator. The app starts with the BikeMate onboarding screens, then opens login. The login screen calls the API and stores JWTs in `SecureStorage`. If the API is offline, seeded email addresses still route to the matching classroom demo shell.

For Visual Studio startup/device selection, open `BikeMate.sln`, right-click `BikeMate.Mobile`, choose **Set as Startup Project**, then select an Android emulator from the debug device dropdown.

## 5. API Keys Needed

Do not put real secrets in source code. Configure these in `BikeMate.Api/appsettings.Development.json`, user secrets, environment variables, or secure storage:

- JWT signing key
- Google OAuth client ID
- SMTP/SendGrid email credentials
- PayMongo public key, secret key, and webhook secret
- Firebase Cloud Messaging credentials
- Cloudinary/Firebase/Azure/local storage settings

## 6. Test Accounts

| Role | Email | Password |
| --- | --- | --- |
| Customer | `customer@bikemate.test` | `Password123!` |
| Mechanic | `mechanic@bikemate.test` | `Password123!` |
| ShopAdmin | `shop@bikemate.test` | `Password123!` |
| SystemAdmin | `admin@bikemate.test` | `Password123!` |

Seed accounts are active and email-verified.

## 7. Modules

- Customer: home, schedule, messages, payments, profile, booking, booking details, tracking, checkout
- Mechanic/Rider: dashboard, jobs, job details, map, messages, history, profile
- System Admin: dashboard, users, mechanic/shop verification, service requests, payments, reports, audit logs
- Shop Admin: dashboard, profile, services, service edit, products, product edit, bookings, mechanics, earnings, schedule

## 8. Troubleshooting

- `database update` requires SQL Server to be running and reachable at the configured connection string.
- This machine uses SQL Server Express, so development is configured for `Server=localhost\SQLEXPRESS;Database=BikeMatesDB_Dev;Trusted_Connection=True;TrustServerCertificate=True;`.
- Stop the API process before rebuilding or running migrations, because a running API can lock `BikeMate.Api.dll`.
- For Android emulator to reach the local API, use `10.0.2.2`, not `localhost`.
- The mobile app uses local HTTPS on port `5001` for emulator development: `https://10.0.2.2:5001/api`.
- The full solution build may show Windows MAUI SDK duplicate-package warnings. The API and Android app build cleanly.
- If the Android emulator is not visible in Visual Studio, install the MAUI workload and Android SDK tools, then create/start a device from **Tools > Android > Android Device Manager**.

## Known Limitations

- Google OAuth, email delivery, PayMongo, Firebase, and file storage are prototype-safe placeholders until real keys are configured.
- MAUI module pages are build-ready prototype screens; deeper API-bound editing forms can be added iteratively.
- The local database was applied successfully to `BikeMatesDB_Dev` on `localhost\SQLEXPRESS` during setup.
