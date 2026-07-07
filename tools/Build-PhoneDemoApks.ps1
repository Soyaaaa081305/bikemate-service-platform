param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [string]$ApiBaseUrl = "https://bikemate-api-afaolandez.azurewebsites.net/api/",
    [switch]$InstallConnectedDevices
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$rootArtifacts = Join-Path $repoRoot "artifacts"
$artifacts = Join-Path $repoRoot "artifacts\phone-demo\apk"
$customerPackage = "com.bikemate.mobile"
$shopAdminPackage = "com.bikemate.shop"

function Ensure-ApiPath {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw "ApiBaseUrl is required."
    }

    $trimmed = $Value.Trim()
    if (-not $trimmed.EndsWith("/")) {
        $trimmed += "/"
    }

    if ($trimmed.EndsWith("/api/", [StringComparison]::OrdinalIgnoreCase)) {
        return $trimmed
    }

    return "$($trimmed.TrimEnd('/'))/api/"
}

$resolvedApiBaseUrl = Ensure-ApiPath $ApiBaseUrl

New-Item -ItemType Directory -Force -Path $rootArtifacts | Out-Null
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null

Push-Location $repoRoot
try {
    Write-Host "Building APKs for API base URL: $resolvedApiBaseUrl"

    Write-Host "Building BikeMate.Mobile APK ($Configuration)..."
    dotnet build .\BikeMate.Mobile\BikeMate.Mobile.csproj -f net10.0-android -c $Configuration -p:AndroidBuildApplicationPackage=true -p:AndroidPackageFormat=apk -p:DebugSymbols=false -p:DebugType=none "-p:BikeMateApiBaseUrl=$resolvedApiBaseUrl"

    Write-Host "Building BIKEMATES_ADMIN APK ($Configuration)..."
    dotnet build .\BIKEMATES_ADMIN\BIKEMATES_ADMIN.csproj -f net10.0-android -c $Configuration -p:AndroidBuildApplicationPackage=true -p:AndroidPackageFormat=apk -p:DebugSymbols=false -p:DebugType=none "-p:BikeMateApiBaseUrl=$resolvedApiBaseUrl"

    $mobileApk = Get-ChildItem .\BikeMate.Mobile\bin\$Configuration\net10.0-android -Recurse -Filter *.apk | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    $adminApk = Get-ChildItem .\BIKEMATES_ADMIN\bin\$Configuration\net10.0-android -Recurse -Filter *.apk | Sort-Object LastWriteTime -Descending | Select-Object -First 1

    if ($null -eq $mobileApk) { throw "BikeMate.Mobile APK was not produced." }
    if ($null -eq $adminApk) { throw "BIKEMATES_ADMIN APK was not produced." }

    $mobileOut = Join-Path $artifacts "BikeMate.Mobile.$Configuration.apk"
    $adminOut = Join-Path $artifacts "BIKEMATES_ADMIN.$Configuration.apk"
    $mobileDemoOut = Join-Path $artifacts "BikeMate.apk"
    $adminDemoOut = Join-Path $artifacts "BikeMate.Shop.apk"
    $mobileRootOut = Join-Path $rootArtifacts "BikeMate.apk"
    $adminRootOut = Join-Path $rootArtifacts "BikeMate.Shop.apk"
    Copy-Item $mobileApk.FullName $mobileOut -Force
    Copy-Item $adminApk.FullName $adminOut -Force
    Copy-Item $mobileApk.FullName $mobileDemoOut -Force
    Copy-Item $adminApk.FullName $adminDemoOut -Force
    Copy-Item $mobileApk.FullName $mobileRootOut -Force
    Copy-Item $adminApk.FullName $adminRootOut -Force

    Write-Host "Customer/mechanic APK: $mobileOut"
    Write-Host "Shop-admin APK: $adminOut"
    Write-Host "Install-friendly customer APK: $mobileRootOut"
    Write-Host "Install-friendly shop APK: $adminRootOut"

    if ($InstallConnectedDevices) {
        $adb = Join-Path $env:LOCALAPPDATA "Android\Sdk\platform-tools\adb.exe"
        if (-not (Test-Path $adb)) {
            throw "adb was not found at $adb"
        }

        $devices = & $adb devices | Select-String "`tdevice$" | ForEach-Object { ($_ -split "`t")[0] }
        if ($devices.Count -eq 0) {
            throw "No connected Android devices were found."
        }

        foreach ($device in $devices) {
            Write-Host "Clearing old app data and installing on $device..."
            & $adb -s $device shell pm clear $customerPackage | Out-Null
            & $adb -s $device shell pm clear $shopAdminPackage | Out-Null
            & $adb -s $device install -r $mobileOut
            & $adb -s $device install -r $adminOut
        }
    }
}
finally {
    Pop-Location
}
