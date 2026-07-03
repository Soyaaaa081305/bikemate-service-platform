# Build Fix Log

Date: 2026-06-09

## Restore

Command:

```powershell
dotnet tool restore
dotnet restore .\BikeMate.slnx
```

Result: Passed.

## Baseline Build

Command:

```powershell
dotnet build .\BikeMate.slnx
```

Result: Passed before code changes.

## Build Error Encountered

Error:

```text
Unable to copy file ... BikeMate.Api.exe because it is being used by another process.
The file is locked by: "BikeMate.Api"
```

Cause:

The API was running in the background during rebuild. Windows locks the running `.exe`, so MSBuild could not overwrite it.

Fix:

```powershell
Stop-Process -Name BikeMate.Api -Force
dotnet build .\BikeMate.slnx
```

Result: Passed.

## Code Fixes Applied

| Area | File | Fix |
| --- | --- | --- |
| App startup | `BikeMate.Mobile\AppShell.xaml` | Launch `MainPage` so onboarding appears |
| Onboarding/login | `BikeMate.Mobile\MainPage.xaml` | Added skip and demo role buttons |
| Login logic | `BikeMate.Mobile\MainViewModel.cs` | Added role selection and safer sign-in behavior |
| Mobile API config | `BikeMate.Mobile\Helpers\ApiConfig.cs` | Switched to HTTPS and debug certificate handler |
| Mobile HTTP setup | `BikeMate.Mobile\MauiProgram.cs` | Added matching debug certificate handler |
| Auth pages | `BikeMate.Mobile\Views\Auth\*.cs` | Reused `ApiConfig.CreateHttpClient()` |
| Android images | `BikeMate.Mobile\BikeMate.Mobile.csproj` | Explicitly included logo/onboarding PNG files as `MauiImage` |
| JWT roles | `BikeMate.Api\Program.cs` | Disabled inbound claim remapping |
| Reports | `BikeMate.Api\Services\BikeMateBackendServices.cs` | Rewrote top services query |

## Final Build

Command:

```powershell
dotnet build .\BikeMate.slnx
```

Result:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

## Visual Studio Android Lock Note

If the Android Debug build fails with:

```text
error XALNS7024: The file is locked by: "Microsoft Visual Studio"
```

Cause:

Visual Studio is still debugging or holding Android deployment files open.

Fix:

1. In Visual Studio, click **Stop Debugging**.
2. If it still fails, close Visual Studio.
3. Re-run:

```powershell
dotnet clean .\BikeMate.Mobile\BikeMate.Mobile.csproj -f net10.0-android
dotnet build .\BikeMate.Mobile\BikeMate.Mobile.csproj -f net10.0-android -t:Run
```

Release build was checked separately and passed:

```powershell
dotnet build .\BikeMate.Mobile\BikeMate.Mobile.csproj -f net10.0-android -c Release
```

After the image fix, Android `mauiimage.inputs`, `mauiimage.outputs`, and `R.txt` include:

- `bikemate_logo`
- `bike_wrench`
- `mechanic_door`
- `running_man`

## Android Build

Command:

```powershell
dotnet build .\BikeMate.Mobile\BikeMate.Mobile.csproj -f net10.0-android
```

Result:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```
