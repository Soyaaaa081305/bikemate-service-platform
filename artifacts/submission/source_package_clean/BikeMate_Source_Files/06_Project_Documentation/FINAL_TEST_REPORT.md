# BikeMate Final Test Report

Date tested: 2026-06-09

Workspace:

```text
C:\Users\Admin\Documents\PROJECTSSS\BikeMate
```

## Summary

Final testing passed for:

- Database migration
- Seed users and lookup data
- API build and run
- Health endpoint
- Login for all four test accounts
- Role-based authorization
- Customer API data
- Mechanic API data
- Shop Admin API data
- System Admin API data
- Chat REST endpoints
- Payment checkout placeholder
- Android project build
- Beginner setup documentation

Device/emulator install was later completed on `emulator-5554` while fixing Android image packaging. The app launched successfully and showed the onboarding images.

## Fixes Applied During Final Testing

### 1. Onboarding and clicks

Problem:

- The original onboarding/summary screen existed in `BikeMate.Mobile\MainPage.xaml`, but the app was launching directly into `LoginPage`.
- This made it look like the original step-by-step app intro was missing.

Fix:

- Updated `BikeMate.Mobile\AppShell.xaml` to launch `MainPage`.
- Added visible `Skip` and demo mode buttons.
- Made the onboarding button text come from the view model so it does not depend on `CarouselView.CurrentItem`.

### 2. Demo mode access

Problem:

- It was not obvious how to access Customer, Mechanic, Shop Admin, or System Admin modes.

Fix:

- Added demo mode buttons on the login screen:
  - Customer
  - Mechanic
  - Shop Admin
  - System Admin

### 3. Mobile API URL

Problem:

- Mobile config used `http://10.0.2.2:5000/api`, but the API redirects HTTP to HTTPS.

Fix:

- Updated `ApiConfig` to use:

```text
Android: https://10.0.2.2:5001/api/
Other:   https://localhost:5001/api/
```

- Added a debug-only local certificate handler for classroom/dev testing.

### 4. Role authorization

Problem:

- Login returned a valid JWT, but role-protected endpoints returned `403 Forbidden`.

Cause:

- ASP.NET JWT handler was remapping role claims, while the app expected the claim name `role`.

Fix:

- Added `options.MapInboundClaims = false;` in `BikeMate.Api\Program.cs`.

### 5. Reports endpoint

Problem:

- `GET /api/reports/top-services` returned `400 Bad Request`.

Cause:

- EF Core could not translate the original LINQ grouping/projection shape.

Fix:

- Rewrote the top services report query using a SQL-translatable join, group, order, and DTO projection.

### 6. Android images

Problem:

- Logo/onboarding PNG files existed in `Resources\Images`, but Android image build outputs were empty and the images did not show in the app.

Fix:

- Explicitly included the four PNG files as `MauiImage` items in `BikeMate.Mobile\BikeMate.Mobile.csproj`.
- Clean rebuilt the Android app.
- Verified on the emulator that the logo and onboarding images render, and that tapping the onboarding button advances to the next slide.

## Commands Run

### Restore tools

```powershell
dotnet tool restore
```

Result: Passed.

### Restore packages

```powershell
dotnet restore .\BikeMate.slnx
```

Result: Passed.

### Build solution

```powershell
dotnet build .\BikeMate.slnx
```

Result: Passed after stopping the running API process that was locking `BikeMate.Api.exe`.

Final result:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

### Apply database migration

```powershell
dotnet tool run dotnet-ef database update --project .\BikeMate.Infrastructure\BikeMate.Infrastructure.csproj --startup-project .\BikeMate.Api\BikeMate.Api.csproj
```

Result: Passed.

EF Core reported:

```text
No migrations were applied. The database is already up to date.
Done.
```

### Run API

```powershell
dotnet run --project .\BikeMate.Api\BikeMate.Api.csproj --launch-profile https
```

Result: Passed.

API listened on:

```text
https://localhost:5001
http://localhost:5000
```

### Android build

```powershell
dotnet build .\BikeMate.Mobile\BikeMate.Mobile.csproj -f net10.0-android
```

Result: Passed.

Final result:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

## Database Verification

Connection used:

```text
Server=localhost\SQLEXPRESS;Database=BikeMatesDB_Dev;Trusted_Connection=True;TrustServerCertificate=True;
```

SQL Server Express was running.

Migration status: up to date.

Table count: 36 base tables.

Seed users verified:

| Email | Status | Email verified |
| --- | --- | --- |
| `customer@bikemate.test` | active | yes |
| `mechanic@bikemate.test` | active | yes |
| `shop@bikemate.test` | active | yes |
| `admin@bikemate.test` | active | yes |

Roles verified:

- Customer
- Mechanic
- ShopAdmin
- SystemAdmin

Request statuses verified:

- pending
- accepted
- rejected
- en_route
- arrived
- in_progress
- completed
- cancelled

Payment statuses verified:

- unpaid
- pending
- paid
- failed
- cancelled
- refunded

Payment methods verified:

- gcash
- maya
- card
- grab_pay
- cash
- bank_transfer

## API Test Results

Base URL used:

```text
https://localhost:5001/api
```

Important: use HTTPS for authenticated requests. HTTP redirects to HTTPS and some tools can drop the bearer token during redirect.

| Test | Result |
| --- | --- |
| `GET /api/health` | Passed |
| Customer login | Passed |
| Mechanic login | Passed |
| Shop Admin login | Passed |
| System Admin login | Passed |
| `GET /api/auth/me` for all roles | Passed |
| `GET /api/customers/me` | Passed |
| `GET /api/customers/motorcycles` | Passed |
| `GET /api/service-requests/upcoming` | Passed |
| `GET /api/service-requests/history` | Passed, returned empty history |
| `GET /api/payments/history` | Passed |
| `GET /api/services/categories` | Passed, 6 categories |
| `GET /api/services/shops` | Passed, 1 shop |
| `GET /api/mechanics/me` | Passed |
| `GET /api/mechanics/jobs` | Passed, 2 jobs |
| `GET /api/mechanics/jobs/active` | Passed |
| `GET /api/shops/my` | Passed |
| `GET /api/shops/1/services` | Passed, 3 services |
| `GET /api/shops/1/products` | Passed, 2 products |
| `GET /api/shops/1/bookings` | Passed, 2 bookings |
| `GET /api/shops/1/earnings` | Passed |
| `GET /api/admin/dashboard` | Passed |
| `GET /api/admin/users` | Passed, 4 users |
| `GET /api/admin/service-requests` | Passed, 2 requests |
| `GET /api/admin/payments` | Passed |
| `GET /api/reports/top-services` | Passed |
| `GET /api/reports/top-mechanics` | Passed |
| `GET /api/conversations` | Passed |
| `GET /api/conversations/1/messages` | Passed |
| `POST /api/conversations/1/messages` | Passed |
| `GET /api/location/request/1/latest` | Passed |
| `POST /api/payments/create-checkout-session` | Passed |

Note: final smoke testing added development-only test chat messages and pending payment checkout records.

## Mobile App Test Results

| Test | Result |
| --- | --- |
| Android build | Passed |
| Onboarding XAML compiles | Passed |
| Role shell XAML compiles | Passed |
| Customer shell exists | Passed |
| Mechanic shell exists | Passed |
| Shop Admin shell exists | Passed |
| System Admin shell exists | Passed |
| Images exist in `Resources\Images` | Passed |
| Images packaged into Android resources | Passed |
| Logo visible on emulator | Passed |
| Onboarding button tap advances slide | Passed |
| Physical emulator/device run | Passed on `emulator-5554` |

Images found and used:

- `bikemate_logo.png`
- `bike_wrench.png`
- `mechanic_door.png`
- `running_man.png`

## How To Run Again

Open PowerShell in:

```powershell
cd "C:\Users\Admin\Documents\PROJECTSSS\BikeMate"
```

Restore:

```powershell
dotnet tool restore
dotnet restore .\BikeMate.slnx
```

Apply database:

```powershell
dotnet tool run dotnet-ef database update --project .\BikeMate.Infrastructure\BikeMate.Infrastructure.csproj --startup-project .\BikeMate.Api\BikeMate.Api.csproj
```

Run API:

```powershell
dotnet run --project .\BikeMate.Api\BikeMate.Api.csproj --launch-profile https
```

In another PowerShell window, build Android:

```powershell
dotnet build .\BikeMate.Mobile\BikeMate.Mobile.csproj -f net10.0-android
```

After opening an emulator, run Android:

```powershell
dotnet build .\BikeMate.Mobile\BikeMate.Mobile.csproj -f net10.0-android -t:Run
```

## Test Accounts

| Mode | Email | Password |
| --- | --- | --- |
| Customer | `customer@bikemate.test` | `Password123!` |
| Mechanic | `mechanic@bikemate.test` | `Password123!` |
| Shop Admin | `shop@bikemate.test` | `Password123!` |
| System Admin | `admin@bikemate.test` | `Password123!` |

## Remaining Limitations

- PayMongo keys are placeholders. Checkout structure works, but real payments require real keys.
- Google login structure exists, but real OAuth requires a real Google client ID.
- Email/OTP logs to development output unless SMTP is configured.
- Firebase notification sending is placeholder/database-only.
- Android device run was not tested because no emulator or phone was attached.
