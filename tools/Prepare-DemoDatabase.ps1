param(
    [Parameter(Mandatory = $true)]
    [string]$SqlServer,

    [Parameter(Mandatory = $true)]
    [string]$Database = "BikeMatesDB_Demo",

    [Parameter(Mandatory = $true)]
    [string]$SqlUser,

    [Parameter(Mandatory = $true)]
    [string]$SqlPassword
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$resetScript = Join-Path $PSScriptRoot "Reset-DevData.sql"
$cloudResetScript = Join-Path ([System.IO.Path]::GetTempPath()) "bikemate-reset-demo-cloud.sql"

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw "sqlcmd is required. Install SQL Server command line tools, then rerun this script."
}

if (-not (Test-Path $resetScript)) {
    throw "Missing seed script: $resetScript"
}

$connectionString = "Server=tcp:$SqlServer,1433;Initial Catalog=$Database;Persist Security Info=False;User ID=$SqlUser;Password=$SqlPassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

Push-Location $repoRoot
try {
    Write-Host "Restoring local tools..."
    dotnet tool restore

    Write-Host "Applying EF migrations to $Database on $SqlServer..."
    $env:ConnectionStrings__DefaultConnection = $connectionString
    dotnet tool run dotnet-ef database update --project .\BikeMate.Infrastructure\BikeMate.Infrastructure.csproj --startup-project .\BikeMate.Api\BikeMate.Api.csproj

    Write-Host "Preparing cloud-safe seed script..."
    $seedSql = Get-Content $resetScript -Raw
    $seedSql = $seedSql -replace "(?s)^USE \[BikeMatesDB_Dev\];\s*GO\s*", ""
    Set-Content -Path $cloudResetScript -Value $seedSql -Encoding UTF8

    Write-Host "Resetting and seeding demo data..."
    sqlcmd -S "tcp:$SqlServer,1433" -d $Database -U $SqlUser -P $SqlPassword -N -C -b -i $cloudResetScript -W

    Write-Host "Demo database is ready."
}
finally {
    Remove-Item Env:\ConnectionStrings__DefaultConnection -ErrorAction SilentlyContinue
    Pop-Location
}
