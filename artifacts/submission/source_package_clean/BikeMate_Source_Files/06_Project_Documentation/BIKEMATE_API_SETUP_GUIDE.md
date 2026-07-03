# BikeMate API Setup Guide

This guide is for local development. Use placeholder values in committed files and keep real secrets in `appsettings.Development.json`, user secrets, environment variables, or your deployment secret store.

## 1. Run The Backend Locally

1. Install SQL Server 2022 or SQL Server Developer Edition.
2. Create or use the database named `BikeMatesDB`.
3. Copy `appsettings.Development.example.json` to `BikeMate.Api/appsettings.Development.json`.
4. Replace only local development values in the copied file.
5. Run the API:

```powershell
dotnet run --project BikeMate.Api\BikeMate.Api.csproj
```

The Android emulator uses `https://10.0.2.2:5001/api/` from `BikeMate.Mobile/Helpers/ApiConfig.cs`.

## 2. Google Maps Platform

Use Google Maps for embedded maps, Places search, route display, ETA, geocoding, and location validation.

1. Open Google Cloud Console.
2. Create or select a project.
3. Enable billing if Google asks for it.
4. Enable these APIs:
   - Maps SDK for Android
   - Places API
   - Directions API
   - Geocoding API
   - Distance Matrix API, if ETA/distance is calculated by Google
5. Create an API key.
6. Restrict the Android key by package name and SHA-1.
7. Store backend map keys in backend configuration only:

```json
{
  "GoogleMaps": {
    "ApiKey": "YOUR_GOOGLE_MAPS_API_KEY"
  }
}
```

8. If you add native Android Maps SDK later, add the Android key to `BikeMate.Mobile/Platforms/Android/AndroidManifest.xml` using a manifest meta-data entry.
9. Test current location from the Customer booking page and Emergency SOS page.
10. Test external Google Maps launch from tracking pages. Opening the external Maps app does not require a Maps SDK key.

## 3. Google Login

1. Create an OAuth consent screen in Google Cloud.
2. Create an Android OAuth client ID using the BikeMate package name and SHA-1.
3. Create a Web OAuth client ID if the backend also validates web-issued Google ID tokens.
4. Put both client IDs in backend configuration:

```json
{
  "GoogleAuth": {
    "AndroidClientId": "YOUR_GOOGLE_ANDROID_CLIENT_ID",
    "WebClientId": "YOUR_GOOGLE_WEB_CLIENT_ID",
    "ClientIds": [
      "YOUR_GOOGLE_ANDROID_CLIENT_ID",
      "YOUR_GOOGLE_WEB_CLIENT_ID"
    ]
  }
}
```

5. MAUI obtains a Google authorization code with PKCE, exchanges it for an ID token, then sends it to `POST /api/auth/google`.
7. Backend validates it, creates or links a BikeMate user, and returns JWT data.

## 4. Email OTP With SendGrid

1. Create a SendGrid account.
2. Verify a sender email or domain.
3. Create an API key.
4. Store the key in backend configuration:

```json
{
  "SendGrid": {
    "ApiKey": "YOUR_SENDGRID_API_KEY",
    "FromEmail": "no-reply@bikemate.local",
    "FromName": "BikeMate"
  }
}
```

5. Registration creates an OTP row in `otp_verifications`.
6. OTP verification should check expiration and consume the OTP.
7. Add resend cooldown/rate limiting before production.
8. Never store SendGrid keys in MAUI.

## 5. PayMongo

1. Create a PayMongo account.
2. Get test public and secret keys.
3. Store the secret key only in the backend:

```json
{
  "PayMongo": {
    "PublicKey": "pk_test_xxxxx",
    "SecretKey": "sk_test_xxxxx",
    "WebhookSecret": "whsec_xxxxx",
    "SuccessUrl": "bikemate://payment-success",
    "CancelUrl": "bikemate://payment-cancelled"
  }
}
```

4. Use `POST /api/payments/create-checkout` or `POST /api/payments/create-checkout-session`.
5. Configure webhook URL as `https://YOUR_API_DOMAIN/api/payments/webhook/paymongo`.
6. Update payment status only from backend webhook or verified backend payment checks.
7. Do not put PayMongo secret keys in MAUI.

## 6. Firebase Cloud Messaging

1. Create a Firebase project.
2. Add an Android app with the BikeMate package name.
3. Download `google-services.json` if your MAUI Firebase package requires it.
4. Configure Firebase Admin SDK on the backend.
5. Store service account configuration securely:

```json
{
  "Firebase": {
    "ProjectId": "YOUR_FIREBASE_PROJECT_ID",
    "ServiceAccountPath": "firebase-service-account.json"
  }
}
```

6. Register mobile device tokens through `POST /api/notifications/register-device-token`.
7. Test booking accepted, emergency accepted, payment successful, and message received notifications.

## 7. File Storage

Use backend-mediated uploads for profile images, motorcycle photos, shop images, chat attachments, before/after photos, and review photos.

```json
{
  "Storage": {
    "Provider": "AzureBlob",
    "ConnectionString": "YOUR_AZURE_BLOB_CONNECTION_STRING",
    "ContainerName": "bikemate-files"
  }
}
```

Do not upload directly from MAUI using storage secrets. The backend should validate file type and size, upload to storage, then save the URL in SQL Server.

## 8. SignalR

BikeMate now maps these hubs:

- `/hubs/booking`
- `/hubs/emergency`
- `/hubs/chat`
- `/hubs/location`
- `/hubs/notification`

Recommended groups:

- `user-{userId}`
- `request-{requestId}`
- `emergency-request-{requestId}`
- `conversation:{conversationId}` for the existing chat hub
- `shop-{shopId}`
- `admin-monitoring`

Mobile clients should connect with the JWT access token, join the relevant group, handle reconnects, and fall back to polling every 3-5 seconds.

## 9. Emergency Call Provider

The emergency live call screen has a placeholder service:

- `IEmergencyCallService`
- `EmergencyCallService`

Future provider options:

- Agora
- Twilio
- WebRTC

Do not claim real audio/video is working until a provider is configured and tested.

## 10. Development Seed Logins

Use only development seed accounts locally. Check `BikeMate.Infrastructure/Data/BikeMateDbContext.cs` for the current seeded users and roles. Do not reuse seed credentials in production.

## 11. Production Safety Checklist

- No secrets in MAUI.
- HTTPS only.
- Strong JWT key.
- Refresh-token flow before production launch.
- OTP/login rate limiting.
- PayMongo webhook signature validation.
- Firebase service account stored outside source control.
- File upload validation.
- Role-based authorization on all module APIs.
- Audit logs for admin/shop sensitive actions.
- Polling fallback when SignalR disconnects.
