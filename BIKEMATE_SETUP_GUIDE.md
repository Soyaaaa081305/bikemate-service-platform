# BikeMate Setup Guide

This guide wires the existing BikeMate solution together for local development. Use placeholders only in committed files. Put real keys in `BikeMate.Api/appsettings.Local.json`, user secrets, environment variables, or provider dashboards.

## 1. Solution Projects

- `BikeMate.Mobile` - .NET MAUI Android app for customer, rider, shop admin, and system admin mobile flows.
- `BikeMate.Api` - ASP.NET Core Web API, SignalR hubs, auth, payments, maps, files, emergency, and role APIs.
- `BikeMate.Core` - shared entities, constants, and DTOs.
- `BikeMate.Infrastructure` - EF Core SQL Server `BikeMateDbContext` and migrations.
- Blazor admin/shop web is not present yet. Add `BikeMate.Web` later and point it at the same API and database.

## 2. SQL Server

1. Install SQL Server Developer or SQL Server Express.
2. Create a local database, for example `BikeMatesDB`.
3. Configure:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=BikeMatesDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

4. Apply migrations:

```powershell
dotnet ef database update --project BikeMate.Infrastructure --startup-project BikeMate.Api
```

## 3. Backend API

Run:

```powershell
dotnet run --project BikeMate.Api\BikeMate.Api.csproj --urls http://localhost:5000
```

For Android emulator access, expose the API with ngrok:

```powershell
ngrok http 5000
```

Then set `BikeMate.Mobile/Helpers/ApiConfig.cs` to the ngrok `/api/` URL during emulator testing.

## 4. JWT

Configure:

```json
{
  "Jwt": {
    "Key": "REPLACE_WITH_LONG_RANDOM_DEV_SECRET",
    "Issuer": "BikeMate",
    "Audience": "BikeMateMobile",
    "ExpiresMinutes": 480
  }
}
```

BikeMate allows a small clock skew for emulator/server time drift. Keep device and PC clocks synced.

## 5. Google Login

In Google Cloud Console:

1. Set OAuth consent app name to `BikeMate`.
2. Set support email.
3. Add development redirect URI for backend flow:

```text
https://YOUR_NGROK_DOMAIN/api/auth/google/callback
```

4. Add Android OAuth client for package:

```text
com.bikemate.mobile
```

5. Configure placeholders:

```json
{
  "GoogleAuth": {
    "WebClientId": "YOUR_WEB_CLIENT_ID",
    "WebClientSecret": "YOUR_WEB_CLIENT_SECRET",
    "AndroidClientId": "YOUR_ANDROID_CLIENT_ID",
    "RedirectUri": "https://YOUR_NGROK_DOMAIN/api/auth/google/callback",
    "MobileCallbackUri": "bikemate://auth/google"
  }
}
```

Google may show the ngrok domain during development because it is the temporary authorized domain. Production branding requires a real domain and verified OAuth consent screen.

## 6. Google Maps

Enable these APIs:

- Maps SDK for Android
- Maps Embed API
- Places API
- Geocoding API
- Directions API
- Distance Matrix API or Routes API

Store backend keys server-side:

```json
{
  "GoogleMaps": {
    "ApiKey": "YOUR_GOOGLE_MAPS_SERVER_KEY"
  }
}
```

Android map metadata lives in `BikeMate.Mobile/Platforms/Android/Resources/values/google_maps_api.xml`. Restrict mobile keys by package name and SHA-1.

## 7. Email OTP

Configure SendGrid or another email provider:

```json
{
  "SendGrid": {
    "ApiKey": "YOUR_SENDGRID_API_KEY",
    "FromEmail": "no-reply@example.com",
    "FromName": "BikeMate"
  }
}
```

Used for account OTP and password reset OTP. Local development logs OTP codes when no provider is configured.

## 8. PayMongo

Configure:

```json
{
  "PayMongo": {
    "SecretKey": "YOUR_PAYMONGO_SECRET_KEY",
    "PublicKey": "YOUR_PAYMONGO_PUBLIC_KEY",
    "WebhookSecret": "YOUR_PAYMONGO_WEBHOOK_SECRET",
    "SuccessReturnUrl": "bikemate://payment-success",
    "CancelReturnUrl": "bikemate://payment-cancelled"
  }
}
```

Payment state must be confirmed by backend webhook before final paid/closed states are trusted.

## 9. Firebase Cloud Messaging

Configure Android `google-services.json` locally and never commit production credentials. API service-account JSON should stay in a local ignored file.

Used for booking, emergency, payment, and chat notifications.

## 10. File Storage

Configure upload root and public base URL:

```json
{
  "Storage": {
    "BaseUrl": "https://YOUR_DOMAIN/uploads",
    "MaxFileBytes": 52428800
  }
}
```

Use backend upload endpoints for profile photos, motorcycle photos, chat attachments, service images, inventory images, and review photos.

## 11. SignalR

Hubs:

- `/hubs/booking`
- `/hubs/chat`
- `/hubs/emergency`
- `/hubs/location`
- `/hubs/notification`

The mobile app should connect with the same JWT used for API calls.

## 12. Optional Agora

Only enable call UI when Agora is configured and a native MAUI binding exists.

```json
{
  "Agora": {
    "AppId": "YOUR_AGORA_APP_ID",
    "PrimaryCertificate": "YOUR_AGORA_CERTIFICATE",
    "TokenLifetimeSeconds": 1800
  }
}
```

Until the native SDK is added, BikeMate should show an in-app support session state, not fake video/audio.

## 13. System Admin Web

Use the single system admin web project:

```powershell
dotnet run --project BikeMate.WebAdmin\BikeMate.WebAdmin\BikeMate.WebAdmin.csproj
```

Use the same API and database. The old `BikeMate.Web` customer/shop web project is not part of the active solution. Suggested routes:

- `/admin/login`, `/admin/dashboard`, `/admin/users`, `/admin/service-requests`, `/admin/emergency-requests`, `/admin/payments`, `/admin/audit-logs`
- `/admin/shops`, `/admin/mechanics`, `/admin/approvals`

Global CSS variables:

```css
:root {
  --font-heading: "PT Sans Caption", sans-serif;
  --font-body: "Public Sans", sans-serif;
  --font-ui: "Inter", sans-serif;
  --color-primary: #ff6b2c;
  --color-emergency: #ff1f1f;
}
```

## 14. MAUI Android

1. Use a Google Play Android emulator image with Chrome enabled.
2. Build:

```powershell
dotnet build BikeMate.Mobile\BikeMate.Mobile.csproj -f net10.0-android
```

3. If Android build outputs are locked, stop Visual Studio debugging/emulator deployment and retry.

## 15. Smoke Tests

Google login:

1. Tap Continue with Google.
2. Complete Google consent.
3. Confirm BikeMate receives JWT.
4. Confirm role-based redirect.

Password reset:

1. Tap Forgot Password.
2. Enter email.
3. Enter reset OTP.
4. Set and confirm new password.
5. Sign in with the new password.

Maps:

1. Open Change Location.
2. Use GPS or search.
3. Confirm location.
4. Confirm booking/emergency uses the selected address.
5. Open tracking and verify directions.

Payments:

1. Complete a service.
2. Confirm status stays payment pending until PayMongo webhook confirms payment.
3. Pay in test mode.
4. Verify receipt uses backend payment data.

Messaging:

1. Open conversations.
2. Send text.
3. Upload image/file through backend upload.
4. Verify receiver sees attachment and timestamps.

Rider:

1. Go online.
2. Accept request.
3. Update job status and live location.
4. Confirm customer tracking updates.

Admin/shop:

1. Log in with each role.
2. Verify dashboards load API data.
3. Verify payments, bookings, emergency requests, and audit logs are not static.
