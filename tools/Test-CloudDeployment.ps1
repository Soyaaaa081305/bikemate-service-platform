param(
    [string]$ApiBaseUrl = "https://bikemate-api-afaolandez.azurewebsites.net/api/",
    [string]$DemoPassword = "Demo123!",
    [switch]$SkipUpload
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Net.Http

$apiBase = $ApiBaseUrl.TrimEnd("/") + "/"
$failures = New-Object System.Collections.Generic.List[string]

function Invoke-Check {
    param(
        [string]$Name,
        [scriptblock]$Script
    )

    try {
        $result = & $Script
        Write-Host "[OK] $Name"
        if ($null -ne $result -and $result -is [string] -and $result.Length -gt 0) {
            Write-Host "     $result"
        }
        return $result
    }
    catch {
        $message = $_.Exception.Message
        $failures.Add("$Name - $message")
        Write-Host "[FAIL] $Name"
        Write-Host "       $message"
        return $null
    }
}

function Invoke-OptionalCheck {
    param(
        [string]$Name,
        [scriptblock]$Script,
        [string]$SkipMessage = "Optional check skipped."
    )

    try {
        $result = & $Script
        Write-Host "[OK] $Name"
        if ($null -ne $result -and $result -is [string] -and $result.Length -gt 0) {
            Write-Host "     $result"
        }
        return $result
    }
    catch {
        Write-Host "[WARN] $Name"
        Write-Host "       $SkipMessage"
        Write-Host "       $($_.Exception.Message)"
        return $null
    }
}

function Invoke-Json {
    param(
        [string]$Method = "GET",
        [string]$Path,
        [object]$Body = $null,
        [string]$Token = ""
    )

    $headers = @{}
    if (-not [string]::IsNullOrWhiteSpace($Token)) {
        $headers["Authorization"] = "Bearer $Token"
    }

    $parameters = @{
        Uri = "$apiBase$Path"
        Method = $Method
        Headers = $headers
        UseBasicParsing = $true
        TimeoutSec = 60
    }

    if ($null -ne $Body) {
        $parameters["ContentType"] = "application/json"
        $parameters["Body"] = ($Body | ConvertTo-Json -Depth 20)
    }

    Invoke-RestMethod @parameters
}

function Login-DemoUser {
    param([string]$Email)

    Invoke-Json -Method POST -Path "auth/login" -Body @{
        email = $Email
        password = $DemoPassword
    }
}

Write-Host "Testing BikeMate API at $apiBase"

Invoke-Check "health" {
    $health = Invoke-Json -Path "health"
    if ($health.status -ne "ok") {
        throw "Unexpected health status: $($health.status)"
    }
    "service=$($health.service)"
} | Out-Null

Invoke-Check "auth availability" {
    $availability = Invoke-Json -Path "auth/availability?email=cloud-smoke-$([guid]::NewGuid().ToString('N'))@bikemate.test&phone=%2B63999$((Get-Random -Minimum 1000000 -Maximum 9999999))"
    if (-not $availability.emailAvailable -or -not $availability.phoneAvailable) {
        throw "Fresh demo email/phone should be available."
    }
} | Out-Null

Invoke-Check "public service categories" {
    $categories = Invoke-Json -Path "services/categories"
    if ($categories.Count -lt 1) {
        throw "Expected at least one service category."
    }
    "count=$($categories.Count)"
} | Out-Null

Invoke-Check "public shops" {
    $shops = Invoke-Json -Path "services/shops"
    if ($shops.Count -lt 1) {
        throw "Expected at least one shop."
    }
    "count=$($shops.Count)"
} | Out-Null

$admin = Invoke-Check "admin login" { Login-DemoUser "isaiahandreinoda@gmail.com" }
$customer = Invoke-Check "customer login" { Login-DemoUser "customer1@bikemate.test" }
$shop = Invoke-Check "shop login" { Login-DemoUser "shop1@bikemate.test" }
$mechanic = Invoke-OptionalCheck "mechanic demo login" `
    { Login-DemoUser "mechanic1@bikemate.test" } `
    "mechanic1@bikemate.test is not required because mechanic demo accounts can be rotated by approvals/reset data."

if ($admin) {
    Invoke-Check "admin dashboard" {
        $dashboard = Invoke-Json -Path "admin/dashboard" -Token $admin.accessToken
        if ($null -eq $dashboard) { throw "No dashboard payload." }
    } | Out-Null

    Invoke-Check "admin mechanics" {
        $mechanics = Invoke-Json -Path "admin/mechanics" -Token $admin.accessToken
        if ($null -eq $mechanics) { throw "No mechanics payload." }
        "count=$($mechanics.Count)"
    } | Out-Null
}

if ($customer) {
    Invoke-Check "customer profile" {
        $me = Invoke-Json -Path "customers/me" -Token $customer.accessToken
        if ($me.email -ne "customer1@bikemate.test") {
            throw "Unexpected customer profile email: $($me.email)"
        }
    } | Out-Null

    Invoke-Check "customer requests" {
        Invoke-Json -Path "service-requests/my" -Token $customer.accessToken | Out-Null
    } | Out-Null
}

if ($shop) {
    Invoke-Check "shop dashboard" {
        Invoke-Json -Path "shop/dashboard" -Token $shop.accessToken | Out-Null
    } | Out-Null
}

if ($mechanic) {
    Invoke-Check "mechanic jobs" {
        Invoke-Json -Path "mechanics/jobs" -Token $mechanic.accessToken | Out-Null
    } | Out-Null
}

if (-not $SkipUpload) {
    Invoke-Check "anonymous onboarding upload" {
        $tempFile = New-TemporaryFile
        try {
            Set-Content -LiteralPath $tempFile.FullName -Value "BikeMate cloud upload smoke test $([DateTime]::UtcNow.ToString("O"))"

            $client = New-Object System.Net.Http.HttpClient
            $client.Timeout = [TimeSpan]::FromSeconds(60)
            $multipart = New-Object System.Net.Http.MultipartFormDataContent
            $fileStream = [System.IO.File]::OpenRead($tempFile.FullName)
            try {
                $multipart.Add((New-Object System.Net.Http.StringContent("shop-applications")), "folder")
                $fileContent = New-Object System.Net.Http.StreamContent($fileStream)
                $fileContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("text/plain")
                $multipart.Add($fileContent, "file", "smoke-upload.txt")

                $response = $client.PostAsync("$($apiBase)files/onboarding-upload", $multipart).GetAwaiter().GetResult()
                $content = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                if (-not $response.IsSuccessStatusCode) {
                    throw "Upload returned HTTP $([int]$response.StatusCode): $content"
                }

                $uploaded = $content | ConvertFrom-Json
            }
            finally {
                if ($null -ne $fileStream) { $fileStream.Dispose() }
                if ($null -ne $multipart) { $multipart.Dispose() }
                if ($null -ne $client) { $client.Dispose() }
            }

            if ([string]::IsNullOrWhiteSpace($uploaded.url)) {
                throw "Upload did not return a URL."
            }

            $fileResponse = Invoke-WebRequest -Uri $uploaded.url -UseBasicParsing -TimeoutSec 60
            if ($fileResponse.StatusCode -ne 200) {
                throw "Uploaded file URL returned HTTP $($fileResponse.StatusCode)."
            }

            $uploaded.url
        }
        finally {
            Remove-Item -LiteralPath $tempFile.FullName -Force -ErrorAction SilentlyContinue
        }
    } | Out-Null
}

if ($failures.Count -gt 0) {
    Write-Host ""
    Write-Host "Cloud deployment smoke test failed:"
    foreach ($failure in $failures) {
        Write-Host " - $failure"
    }
    exit 1
}

Write-Host ""
Write-Host "Cloud deployment smoke test passed."
