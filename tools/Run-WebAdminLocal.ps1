param(
    [int]$Port = 5266
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "BikeMate.WebAdmin\BikeMate.WebAdmin\BikeMate.WebAdmin.csproj"
$outputDir = Join-Path $repoRoot "BikeMate.WebAdmin\BikeMate.WebAdmin\bin\Debug\net10.0"
$subject = "CN=BikeMate Local Dev Code Signing"

$listener = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
if ($listener) {
    Stop-Process -Id $listener.OwningProcess -Force
    Start-Sleep -Seconds 1
}

dotnet build $projectPath -v:minimal

$cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert |
    Where-Object { $_.Subject -eq $subject } |
    Sort-Object NotAfter -Descending |
    Select-Object -First 1

if (-not $cert) {
    $cert = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject $subject `
        -CertStoreLocation Cert:\CurrentUser\My `
        -KeyUsage DigitalSignature `
        -KeyExportPolicy Exportable `
        -KeyAlgorithm RSA `
        -KeyLength 3072 `
        -HashAlgorithm SHA256 `
        -NotAfter (Get-Date).AddYears(2)
}

$tempCert = Join-Path $env:TEMP "BikeMateLocalDevCodeSigning.cer"
Export-Certificate -Cert $cert -FilePath $tempCert -Force | Out-Null
Import-Certificate -FilePath $tempCert -CertStoreLocation Cert:\CurrentUser\TrustedPublisher | Out-Null
Import-Certificate -FilePath $tempCert -CertStoreLocation Cert:\CurrentUser\Root | Out-Null

Get-ChildItem -LiteralPath $outputDir -File |
    Where-Object { $_.Name -like "BikeMate*.dll" -or $_.Name -like "BikeMate*.exe" } |
    ForEach-Object {
        $signature = Set-AuthenticodeSignature -FilePath $_.FullName -Certificate $cert -HashAlgorithm SHA256
        if ($signature.Status -notin @("Valid", "HashMismatch")) {
            Write-Warning "Signature status for $($_.Name): $($signature.Status) - $($signature.StatusMessage)"
        }
    }

$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --no-build --project $projectPath --urls "http://localhost:$Port"
