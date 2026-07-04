param(
    [string]$AvdName = "pixel_7_-_api_35_0",
    [ValidateSet("Mobile", "Admin", "Both")]
    [string]$App = "Mobile",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$SkipBuild,
    [switch]$NoClearData
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$androidSdk = Join-Path $env:LOCALAPPDATA "Android\Sdk"
$adb = Join-Path $androidSdk "platform-tools\adb.exe"
$emulator = Join-Path $androidSdk "emulator\emulator.exe"

$mobilePackage = "com.bikemate.mobile"
$adminPackage = "com.bikemate.shop"

function Require-File {
    param(
        [string]$Path,
        [string]$Message
    )

    if (-not (Test-Path $Path)) {
        throw $Message
    }
}

function Get-ConnectedDeviceIds {
    & $adb devices |
        Select-String "`tdevice$" |
        ForEach-Object { ($_ -split "`t")[0] }
}

function Wait-For-Boot {
    Write-Host "Waiting for Android device..."
    & $adb wait-for-device

    $deadline = (Get-Date).AddMinutes(5)
    do {
        $booted = (& $adb shell getprop sys.boot_completed 2>$null | Select-Object -First 1).Trim()
        if ($booted -eq "1") {
            Write-Host "Android device is ready."
            return
        }

        Start-Sleep -Seconds 5
    } while ((Get-Date) -lt $deadline)

    throw "Timed out waiting for Android emulator to finish booting."
}

function Find-Apk {
    param(
        [string]$ProjectFolder
    )

    $apkRoot = Join-Path $repoRoot "$ProjectFolder\bin\$Configuration\net10.0-android"
    $apk = Get-ChildItem $apkRoot -Recurse -Filter "*Signed.apk" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($null -eq $apk) {
        $apk = Get-ChildItem $apkRoot -Recurse -Filter "*.apk" -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1
    }

    if ($null -eq $apk) {
        throw "No APK found under $apkRoot. Build the project first or run without -SkipBuild."
    }

    return $apk.FullName
}

function Install-And-Launch {
    param(
        [string]$PackageName,
        [string]$ApkPath
    )

    if (-not $NoClearData) {
        Write-Host "Clearing app data for $PackageName..."
        & $adb shell pm clear $PackageName | Out-Null
    }

    Write-Host "Installing $ApkPath..."
    & $adb install -r $ApkPath
    if ($LASTEXITCODE -ne 0) {
        throw "APK install failed for $PackageName."
    }

    Write-Host "Launching $PackageName..."
    & $adb shell monkey -p $PackageName 1 | Out-Null
}

Require-File -Path $adb -Message "adb was not found at $adb. Install Android SDK Platform Tools."
Require-File -Path $emulator -Message "emulator.exe was not found at $emulator. Install Android Emulator from Android Studio."

Push-Location $repoRoot
try {
    if (-not $SkipBuild) {
        if ($App -in @("Mobile", "Both")) {
            Write-Host "Building BikeMate.Mobile ($Configuration)..."
            dotnet build .\BikeMate.Mobile\BikeMate.Mobile.csproj -f net10.0-android -c $Configuration
            if ($LASTEXITCODE -ne 0) { throw "BikeMate.Mobile build failed." }
        }

        if ($App -in @("Admin", "Both")) {
            Write-Host "Building BIKEMATES_ADMIN ($Configuration)..."
            dotnet build .\BIKEMATES_ADMIN\BIKEMATES_ADMIN.csproj -f net10.0-android -c $Configuration
            if ($LASTEXITCODE -ne 0) { throw "BIKEMATES_ADMIN build failed." }
        }
    }

    $devices = @(Get-ConnectedDeviceIds)
    if ($devices.Count -eq 0) {
        $availableAvds = @(& $emulator -list-avds)
        if ($availableAvds -notcontains $AvdName) {
            throw "AVD '$AvdName' was not found. Available AVDs: $($availableAvds -join ', ')"
        }

        Write-Host "Starting emulator: $AvdName"
        Start-Process -FilePath $emulator -ArgumentList @("-avd", $AvdName)
        Wait-For-Boot
    }
    else {
        Write-Host "Using connected device: $($devices[0])"
    }

    if ($App -in @("Mobile", "Both")) {
        $mobileApk = Find-Apk -ProjectFolder "BikeMate.Mobile"
        Install-And-Launch -PackageName $mobilePackage -ApkPath $mobileApk
    }

    if ($App -in @("Admin", "Both")) {
        $adminApk = Find-Apk -ProjectFolder "BIKEMATES_ADMIN"
        Install-And-Launch -PackageName $adminPackage -ApkPath $adminApk
    }

    Write-Host "Android demo app is ready."
}
finally {
    Pop-Location
}
