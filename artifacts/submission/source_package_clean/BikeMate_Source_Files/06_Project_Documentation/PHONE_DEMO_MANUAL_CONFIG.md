# BikeMate Phone Demo Manual Config

This is the remaining manual setup needed for a phone-only demo.

## Already configured in repo

- Customer/mechanic Android API default: `https://bikemate-api-demo.azurewebsites.net/api/`
- Shop-admin Android API default: `https://bikemate-api-demo.azurewebsites.net/api/`
- Agora App ID and certificates are set in local API/WebAdmin config and in `tools/Deploy-PhoneDemo.ps1`.
- Philippine geography uses the API endpoint `geography/*`; the API falls back to built-in regions if the external PSGC provider is unavailable.

## Required for cloud demo

Create these manually first:

- Azure account with free resources enabled.
- Azure SQL free database: `BikeMatesDB_Demo`.
- SQL server name, SQL admin username, SQL admin password.
- Azure CLI installed and logged in with `az login`.

Then run:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\Prepare-DemoDatabase.ps1 -SqlServer "<server>.database.windows.net" -Database "BikeMatesDB_Demo" -SqlUser "<sql-user>" -SqlPassword "<sql-password>"

powershell -ExecutionPolicy Bypass -File .\tools\Deploy-PhoneDemo.ps1 -SqlServer "<server>.database.windows.net" -SqlDatabase "BikeMatesDB_Demo" -SqlUser "<sql-user>" -SqlPassword "<sql-password>"
```

## Optional provider keys

These are only needed if you want the related feature live in the demo:

- Google Maps API key: maps, geocoding, directions.
- Google OAuth Web Client ID, Android Client ID, Web Client Secret: Google sign-in.
- SendGrid API key and verified From email: real OTP/email delivery.
- PayMongo public key, secret key, webhook secret: real test checkout/payment callbacks.
- Firebase project/service account: push notifications, if enabled.

Without these optional keys:

- Email OTP may not send real email.
- Google sign-in will not work.
- PayMongo checkout may fail or stay in mock/test fallback depending on the code path.
- Maps may load without full geocoding/directions support.

## Build APKs

After the public API URL is correct:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\Build-PhoneDemoApks.ps1
```

Clean old app data on phones before testing so old API URL overrides do not survive.
