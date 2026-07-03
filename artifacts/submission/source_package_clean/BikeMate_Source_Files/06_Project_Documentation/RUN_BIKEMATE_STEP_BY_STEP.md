# Run BikeMate Step By Step

This guide is for running BikeMate locally on this Windows computer.

## 1. Open the project folder

Open PowerShell and go to the BikeMate folder:

```powershell
cd "C:\Users\Admin\Documents\PROJECTSSS\BikeMate"
```

You should see files like `BikeMate.slnx`, `BikeMate.Api`, `BikeMate.Mobile`, `BikeMate.Core`, and `BikeMate.Infrastructure`.

## 2. Check .NET and workloads

Run:

```powershell
dotnet --version
dotnet --list-sdks
dotnet workload list
```

This project currently targets `.NET 10`:

- API/Core/Infrastructure: `net10.0`
- Android app: `net10.0-android`

This computer has SDK `10.0.300`, `maui`, and `android` workloads installed.

If MAUI is missing on another computer, install it with:

```powershell
dotnet workload install maui
```

## 3. Restore the EF Core tool and packages

Run these in the project folder:

```powershell
dotnet tool restore
dotnet restore .\BikeMate.slnx
```

Successful output should say the `dotnet-ef` tool was restored and all projects are up to date.

## 4. Start SQL Server

This project is configured for SQL Server Express:

```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=BikeMatesDB_Dev;Trusted_Connection=True;TrustServerCertificate=True;"
```

To check SQL Server Express:

```powershell
Get-Service -Name 'MSSQL$SQLEXPRESS'
```

If it is stopped, start it from Windows Services or run PowerShell as Administrator and use:

```powershell
Start-Service -Name 'MSSQL$SQLEXPRESS'
```

## 5. Apply the database migration

Run:

```powershell
dotnet tool run dotnet-ef database update --project .\BikeMate.Infrastructure\BikeMate.Infrastructure.csproj --startup-project .\BikeMate.Api\BikeMate.Api.csproj
```

Successful output should include:

```text
Build succeeded.
Done.
```

If it says the database is already up to date, that is okay.

## 6. Run the backend API

Run:

```powershell
dotnet run --project .\BikeMate.Api\BikeMate.Api.csproj --launch-profile https
```

Successful output should show:

```text
Now listening on: https://localhost:5001
Now listening on: http://localhost:5000
```

Leave this PowerShell window open while using the mobile app.

## 7. Test the health endpoint

Open another PowerShell window in the same project folder and run:

```powershell
Invoke-RestMethod -Uri "https://localhost:5001/api/health"
```

Successful output should include:

```text
status  : ok
service : BikeMate.Api
```

Use `https://localhost:5001` for authenticated API testing. The HTTP URL redirects to HTTPS, and some tools drop the login token during redirects.

## 8. Build the Android app

Run:

```powershell
dotnet build .\BikeMate.Mobile\BikeMate.Mobile.csproj -f net10.0-android
```

Successful output should say:

```text
Build succeeded.
0 Error(s)
```

## 9. Open an Android emulator

Use Visual Studio:

1. Open `BikeMate.sln`.
2. Open Android Device Manager.
3. Start an Android emulator.
4. Wait until the emulator is fully loaded.

To check devices from PowerShell:

```powershell
& "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe" devices
```

You should see a device listed. If it only says `List of devices attached` with nothing under it, no emulator or phone is connected yet.

## 10. Run the MAUI Android app

After an emulator is running:

```powershell
dotnet build .\BikeMate.Mobile\BikeMate.Mobile.csproj -f net10.0-android -t:Run
```

The app starts on the BikeMate onboarding screens. Use `Next`, `Start`, or `Skip` to reach login.

## 11. Access the different app modes

On the login screen, tap one of the demo mode buttons:

- Customer
- Mechanic
- Shop Admin
- System Admin

Then tap `Sign In`.

You can also type the accounts manually:

| Mode | Email | Password |
| --- | --- | --- |
| Customer | `customer@bikemate.test` | `Password123!` |
| Mechanic | `mechanic@bikemate.test` | `Password123!` |
| Shop Admin | `shop@bikemate.test` | `Password123!` |
| System Admin | `admin@bikemate.test` | `Password123!` |

If the API is running, login uses the real backend and JWT token.

If the API is not running, the app opens a clearly labeled classroom demo mode based on the selected email.

## 12. What each mode opens

- Customer opens `CustomerShell`: Home, Schedule, Messages, Payments.
- Mechanic opens `MechanicShell`: Dashboard, Jobs, Messages, History, Profile.
- Shop Admin opens `ShopAdminShell`: Dashboard, Profile, Services, Products, Bookings.
- System Admin opens `AdminShell`: Dashboard, Users, Requests, Payments, Reports.

## 13. Common errors

### Build says the API exe is locked

Cause: the API is still running.

Fix:

```powershell
Get-Process BikeMate.Api -ErrorAction SilentlyContinue
Stop-Process -Name BikeMate.Api -Force
dotnet build .\BikeMate.slnx
```

### Login works but protected API calls fail from HTTP

Cause: `http://localhost:5000` redirects to `https://localhost:5001`, and the token can be dropped during redirect.

Fix: use:

```text
https://localhost:5001/api
```

The Android app is configured to use:

```text
https://10.0.2.2:5001/api
```

### Android cannot connect to API

Check:

1. API is running.
2. The API says it is listening on `https://localhost:5001`.
3. The app is using `https://10.0.2.2:5001/api`.
4. You rebuilt the mobile app after changes.

### No emulator listed

Run:

```powershell
& "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe" devices
```

If no device is listed, start an emulator from Visual Studio Android Device Manager.
