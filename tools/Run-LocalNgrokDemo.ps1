param(
    [int]$ApiPort = 5000,
    [string]$AvdName = "pixel_7_-_api_35_0",
    [ValidateSet("Mobile", "Admin", "Both")]
    [string]$App = "Mobile",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$SkipBuild,
    [switch]$NoClearData
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$apiProject = Join-Path $repoRoot "BikeMate.Api\BikeMate.Api.csproj"
$androidScript = Join-Path $PSScriptRoot "Run-AndroidDemo.ps1"

function Test-PortListening {
    param([int]$Port)

    return $null -ne (Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1)
}

function Wait-For-Url {
    param(
        [string]$Url,
        [int]$TimeoutSeconds = 60
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        try {
            Invoke-RestMethod -Uri $Url -TimeoutSec 3 | Out-Null
            return
        }
        catch {
            Start-Sleep -Seconds 2
        }
    } while ((Get-Date) -lt $deadline)

    throw "Timed out waiting for $Url."
}

function Get-NgrokPublicUrl {
    try {
        $response = Invoke-RestMethod -Uri "http://127.0.0.1:4040/api/tunnels" -TimeoutSec 3
        $tunnel = @($response.tunnels) |
            Where-Object { $_.public_url -like "https://*" } |
            Select-Object -First 1

        if ($null -ne $tunnel) {
            return $tunnel.public_url
        }
    }
    catch {
        return $null
    }

    return $null
}

function Ensure-ApiPath {
    param([string]$Value)

    $trimmed = $Value.Trim()
    if (-not $trimmed.EndsWith("/")) {
        $trimmed += "/"
    }

    if ($trimmed.EndsWith("/api/", [StringComparison]::OrdinalIgnoreCase)) {
        return $trimmed
    }

    return "$($trimmed.TrimEnd('/'))/api/"
}

if (-not (Get-Command ngrok -ErrorAction SilentlyContinue)) {
    throw "ngrok was not found on PATH."
}

if (-not (Test-PortListening -Port $ApiPort)) {
    Write-Host "Starting BikeMate.Api locally on http://localhost:$ApiPort..."
    $apiCommand = "`$env:ASPNETCORE_ENVIRONMENT='Development'; dotnet run --project `"$apiProject`" --urls `"http://localhost:$ApiPort`""
    Start-Process -FilePath "powershell" -ArgumentList @("-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", $apiCommand) -WindowStyle Hidden | Out-Null
}
else {
    Write-Host "Using existing local API listener on port $ApiPort."
}

Wait-For-Url -Url "http://localhost:$ApiPort/health" -TimeoutSeconds 90

$ngrokUrl = Get-NgrokPublicUrl
if ([string]::IsNullOrWhiteSpace($ngrokUrl)) {
    Write-Host "Starting ngrok tunnel to local API port $ApiPort..."
    Start-Process -FilePath "ngrok" -ArgumentList @("http", $ApiPort.ToString()) -WindowStyle Hidden | Out-Null
    $deadline = (Get-Date).AddSeconds(45)
    do {
        Start-Sleep -Seconds 2
        $ngrokUrl = Get-NgrokPublicUrl
    } while ([string]::IsNullOrWhiteSpace($ngrokUrl) -and (Get-Date) -lt $deadline)
}
else {
    Write-Host "Using existing ngrok tunnel: $ngrokUrl"
}

if ([string]::IsNullOrWhiteSpace($ngrokUrl)) {
    throw "ngrok did not expose a public HTTPS URL. Check http://127.0.0.1:4040."
}

$apiBaseUrl = Ensure-ApiPath $ngrokUrl
Write-Host "BikeMate API tunnel: $apiBaseUrl"

& $androidScript `
    -AvdName $AvdName `
    -App $App `
    -Configuration $Configuration `
    -ApiBaseUrl $apiBaseUrl `
    -SkipBuild:$SkipBuild `
    -NoClearData:$NoClearData
