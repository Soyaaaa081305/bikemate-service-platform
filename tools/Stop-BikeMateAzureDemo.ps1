param(
    [string]$ResourceGroup = "BikeMateDemoRG"
)

$ErrorActionPreference = "Stop"

$az = Join-Path $env:ProgramFiles "Microsoft SDKs\Azure\CLI2\wbin\az.cmd"
if (-not (Test-Path $az)) {
    $az = "az"
}

Write-Host "Stopping BikeMate API and WebAdmin apps..."
& $az webapp stop --resource-group $ResourceGroup --name bikemate-api-demo | Out-Null
& $az webapp stop --resource-group $ResourceGroup --name bikemate-webadmin-demo | Out-Null

Write-Host "BikeMate cloud apps are stopped. The SQL serverless database can auto-pause after its idle window."
& $az webapp list --resource-group $ResourceGroup --query "[].{name:name,state:state}" -o table
