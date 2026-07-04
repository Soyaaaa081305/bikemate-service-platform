param(
    [switch]$IncludeUntracked
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$sensitivePathPattern = '(^|/)(\.env|appsettings\.Local\.json|google-services\.json|firebase-service-account.*\.json)$|(^|/)artifacts/|\.apk$|\.aab$|\.zip$'
$secretPatterns = @(
    @{ Name = "Google API key"; Pattern = 'AIza[0-9A-Za-z_-]{20,}' },
    @{ Name = "SendGrid API key"; Pattern = 'SG\.[0-9A-Za-z_-]{10,}\.[0-9A-Za-z_-]{20,}' },
    @{ Name = "PayMongo secret key"; Pattern = '(?<![A-Za-z0-9_])sk_(test|live)_[0-9A-Za-z]{12,}' },
    @{ Name = "PayMongo public key"; Pattern = '(?<![A-Za-z0-9_])pk_(test|live)_[0-9A-Za-z]{12,}' },
    @{ Name = "PayMongo webhook secret"; Pattern = '(?<![A-Za-z0-9_])whsk_[0-9A-Za-z]{12,}' },
    @{ Name = "Google OAuth client secret"; Pattern = 'GOCSPX-[0-9A-Za-z_-]{12,}' },
    @{ Name = "Ngrok auth token"; Pattern = 'ngrok_[0-9A-Za-z]{20,}' },
    @{ Name = "Private key block"; Pattern = ('-----BEGIN ' + 'PRIVATE KEY-----') }
)

function Get-TrackedFiles {
    git ls-files
}

function Get-UntrackedCommitCandidates {
    git ls-files --others --exclude-standard
}

function Test-TextFile {
    param([string]$Path)

    $bytes = [System.IO.File]::ReadAllBytes((Join-Path $repoRoot $Path))
    if ($bytes.Length -eq 0) {
        return $true
    }

    $sampleLength = [Math]::Min($bytes.Length, 8000)
    for ($i = 0; $i -lt $sampleLength; $i++) {
        if ($bytes[$i] -eq 0) {
            return $false
        }
    }

    return $true
}

Push-Location $repoRoot
try {
    $failures = New-Object System.Collections.Generic.List[string]
    $files = @(Get-TrackedFiles)

    if ($IncludeUntracked) {
        $files += @(Get-UntrackedCommitCandidates)
    }

    foreach ($file in ($files | Sort-Object -Unique)) {
        if ($file -match $sensitivePathPattern) {
            $failures.Add("Sensitive file/path is tracked or commit-eligible: $file")
            continue
        }

        if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
            continue
        }

        if (-not (Test-TextFile -Path $file)) {
            continue
        }

        $content = Get-Content -Raw -LiteralPath $file -ErrorAction SilentlyContinue
        foreach ($entry in $secretPatterns) {
            if ([regex]::IsMatch($content, $entry.Pattern)) {
                $failures.Add("$($entry.Name) pattern found in $file")
            }
        }
    }

    if ($failures.Count -gt 0) {
        $failures | Sort-Object -Unique
        throw "Secret hygiene check failed."
    }

    Write-Host "Secret hygiene check passed."
}
finally {
    Pop-Location
}
