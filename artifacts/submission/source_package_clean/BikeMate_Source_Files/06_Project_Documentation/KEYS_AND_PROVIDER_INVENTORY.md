# BikeMate Keys And Provider Inventory

This file is safe to keep in the repo. Private values are redacted here; full local values are stored in ignored local config files on the development machine.

## Runtime URLs

| Provider | Value | Source | Used In |
| --- | --- | --- | --- |
| Ngrok public API URL | `https://hungrily-imagines-suffering.ngrok-free.dev` | User-provided ngrok session | `BikeMate.Mobile/Helpers/ApiConfig.cs`, `BikeMate.Api/appsettings.Local.json` |
| Ngrok local forward target | `http://localhost:5000` | User-provided ngrok session | Local API run command |
| PayMongo webhook endpoint | `https://hungrily-imagines-suffering.ngrok-free.dev/api/payments/webhook/paymongo` | Derived from ngrok URL + API route | PayMongo dashboard webhook URL |

## Google And Firebase

| Provider | Key/Identifier | Source | Used In |
| --- | --- | --- | --- |
| Android package name | `com.bikemate.mobile` | User-provided Firebase package | `BikeMate.Mobile/BikeMate.Mobile.csproj`, Firebase app |
| Google Maps SDK Android API key | `AIzaSyBcB9...PK5I` | User-provided maps key | `BikeMate.Mobile/Platforms/Android/Resources/values/google_maps_api.xml`, `BikeMate.Api/appsettings.Local.json` |
| Google OAuth Android client ID | `1049211486363-l99ohnd6i2e4evptm2d4a39lqt0q58l4.apps.googleusercontent.com` | User-provided OAuth client | `BikeMate.Mobile/Helpers/GoogleAuthConfig.cs`, `BikeMate.Api/appsettings.Local.json` |
| Google OAuth web client ID | `1049211486363-kv5dfofjemltfhip5dif15lqqsfo4olb.apps.googleusercontent.com` | User-provided OAuth client | `BikeMate.Mobile/Helpers/GoogleAuthConfig.cs`, `BikeMate.Api/appsettings.Local.json` |
| Firebase project ID | `bikemate-a228c` | User-provided Firebase JSON | `BikeMate.Mobile/Platforms/Android/google-services.json`, `BikeMate.Api/appsettings.Local.json` |
| Firebase Android API key | `AIzaSyBWB8...n9g4` | User-provided Firebase JSON | `BikeMate.Mobile/Platforms/Android/google-services.json` |
| Android SHA-1 | `FB:80:51:02:77:B2:06:0E:BA:7A:84:8A:25:79:8D:47:7E:1D:1C:3F` | User-provided fingerprint | Firebase/Google Cloud console |
| Android SHA-256 | `25:2C:19:A8:B7:6C:18:BE:D6:20:F3:CC:DE:E0:58:4B:ED:E9:73:CC:91:77:07:E1:5A:7E:C8:5E:14:35:7C:FC` | User-provided fingerprint | Firebase/Google Cloud console |
| Firebase service account private key | Redacted | User-provided service account JSON | `BikeMate.Api/firebase-service-account.local.json` |

## Email

| Provider | Key/Identifier | Source | Used In |
| --- | --- | --- | --- |
| SendGrid API key | `SG.7mB...OJA7s` | User-provided SendGrid key | `BikeMate.Api/appsettings.Local.json` |
| SendGrid from email | `isaiahandreinoda@gmail.com` | User-provided sender email | `BikeMate.Api/appsettings.Local.json` |

## Payments

| Provider | Key/Identifier | Source | Used In |
| --- | --- | --- | --- |
| PayMongo public key | `pk_test_h3mrJH27VUKuGbY7GPJtCVAV` | User-provided PayMongo key | `BikeMate.Api/appsettings.Local.json` |
| PayMongo secret key | `sk_test_hrTE...ShxZ` | User-provided PayMongo key | `BikeMate.Api/appsettings.Local.json` |
| PayMongo webhook secret | `whsk_rQb...H5A2` | User-provided webhook secret | `BikeMate.Api/appsettings.Local.json`, webhook signature verification |

## Realtime / Calls

| Provider | Key/Identifier | Source | Used In |
| --- | --- | --- | --- |
| Agora app ID | `fa02725c...e3f7` | User-provided Agora app ID | `BikeMate.Api/appsettings.Local.json` |
| Agora primary certificate | `033f1344...0c2a` | User-provided Agora certificate | `BikeMate.Api/appsettings.Local.json` |

## Local Full-Value Files

These files are intentionally ignored by git:

- `BikeMate.Api/appsettings.Local.json`
- `BikeMate.Api/firebase-service-account.local.json`
- `BikeMate.Mobile/Platforms/Android/google-services.json`
- `LOCAL_KEYS_INVENTORY.full.local.md`

## Still Needed To Run Cleanly

- Run the API on `http://localhost:5000` while ngrok forwards to it.
- Keep ngrok online at `https://hungrily-imagines-suffering.ngrok-free.dev`, or update `BikeMate.Mobile/Helpers/ApiConfig.cs` when the URL changes.
- Add the PayMongo webhook URL in the PayMongo dashboard: `https://hungrily-imagines-suffering.ngrok-free.dev/api/payments/webhook/paymongo`.
- Confirm `isaiahandreinoda@gmail.com` is a verified SendGrid sender or verified domain sender.
- Confirm Firebase/Google Cloud has package `com.bikemate.mobile` with the SHA-1 and SHA-256 above.
- Add the Agora mobile SDK/token flow before real in-app audio/video calls; the keys are stored, but the emergency call screen is still provider-ready rather than fully Agora-native.
