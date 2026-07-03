# Beginner Requirements Checklist

Use this checklist before running BikeMate.

## Required On This Computer

### 1. .NET SDK

Check:

```powershell
dotnet --version
dotnet --list-sdks
```

This project currently uses `.NET 10`.

Tested SDK on this computer:

```text
10.0.300
```

### 2. .NET MAUI and Android workloads

Check:

```powershell
dotnet workload list
```

Required workloads:

- `maui`
- `android`

If missing:

```powershell
dotnet workload install maui
```

### 3. EF Core tool

This project uses a local tool manifest.

Restore:

```powershell
dotnet tool restore
```

Expected tool:

```text
dotnet-ef
```

### 4. SQL Server

This computer uses:

```text
SQL Server Express: localhost\SQLEXPRESS
```

Check:

```powershell
Get-Service -Name 'MSSQL$SQLEXPRESS'
```

The service should be `Running`.

### 5. HTTPS development certificate

Run:

```powershell
dotnet dev-certs https --trust
```

Why: the API runs on `https://localhost:5001`, and authenticated testing should use HTTPS.

### 6. Android SDK and emulator

Check whether `adb` exists:

```powershell
Test-Path "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
```

Check attached devices:

```powershell
& "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe" devices
```

If no device appears, open Visual Studio Android Device Manager and start an emulator.

## Optional Keys For Real Integrations

BikeMate runs locally without these, but real integrations need keys:

- PayMongo secret/public/webhook keys
- Google OAuth client ID
- SMTP email username/password or SendGrid key
- Firebase credentials

Put development placeholders or local values in:

```text
BikeMate.Api\appsettings.Development.json
```

Never put real secrets in the MAUI mobile app.
