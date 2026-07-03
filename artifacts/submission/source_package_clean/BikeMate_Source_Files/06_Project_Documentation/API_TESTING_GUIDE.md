# API Testing Guide

Use this after the API is running.

Start the API first:

```powershell
dotnet run --project .\BikeMate.Api\BikeMate.Api.csproj --launch-profile https
```

Use this base URL:

```text
https://localhost:5001/api
```

## Health

```powershell
Invoke-RestMethod -Uri "https://localhost:5001/api/health"
```

Expected result includes:

```text
status : ok
service : BikeMate.Api
```

## Login

Customer:

```powershell
$base = "https://localhost:5001/api"
$body = @{ email = "customer@bikemate.test"; password = "Password123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$base/auth/login" -Method Post -ContentType "application/json" -Body $body
$token = $auth.accessToken
$headers = @{ Authorization = "Bearer $token" }
```

Current user:

```powershell
Invoke-RestMethod -Uri "$base/auth/me" -Headers $headers
```

## Customer Endpoints

```powershell
Invoke-RestMethod -Uri "$base/customers/me" -Headers $headers
Invoke-RestMethod -Uri "$base/customers/motorcycles" -Headers $headers
Invoke-RestMethod -Uri "$base/service-requests/upcoming" -Headers $headers
Invoke-RestMethod -Uri "$base/payments/history" -Headers $headers
Invoke-RestMethod -Uri "$base/conversations" -Headers $headers
```

## Mechanic Endpoints

```powershell
$body = @{ email = "mechanic@bikemate.test"; password = "Password123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$base/auth/login" -Method Post -ContentType "application/json" -Body $body
$headers = @{ Authorization = "Bearer $($auth.accessToken)" }

Invoke-RestMethod -Uri "$base/mechanics/me" -Headers $headers
Invoke-RestMethod -Uri "$base/mechanics/jobs" -Headers $headers
Invoke-RestMethod -Uri "$base/mechanics/jobs/active" -Headers $headers
```

## Shop Admin Endpoints

```powershell
$body = @{ email = "shop@bikemate.test"; password = "Password123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$base/auth/login" -Method Post -ContentType "application/json" -Body $body
$headers = @{ Authorization = "Bearer $($auth.accessToken)" }

Invoke-RestMethod -Uri "$base/shops/my" -Headers $headers
Invoke-RestMethod -Uri "$base/shops/1/services" -Headers $headers
Invoke-RestMethod -Uri "$base/shops/1/products" -Headers $headers
Invoke-RestMethod -Uri "$base/shops/1/bookings" -Headers $headers
Invoke-RestMethod -Uri "$base/shops/1/earnings" -Headers $headers
```

## System Admin Endpoints

```powershell
$body = @{ email = "admin@bikemate.test"; password = "Password123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$base/auth/login" -Method Post -ContentType "application/json" -Body $body
$headers = @{ Authorization = "Bearer $($auth.accessToken)" }

Invoke-RestMethod -Uri "$base/admin/dashboard" -Headers $headers
Invoke-RestMethod -Uri "$base/admin/users" -Headers $headers
Invoke-RestMethod -Uri "$base/admin/service-requests" -Headers $headers
Invoke-RestMethod -Uri "$base/admin/payments" -Headers $headers
Invoke-RestMethod -Uri "$base/reports/top-services" -Headers $headers
Invoke-RestMethod -Uri "$base/reports/top-mechanics" -Headers $headers
```

## Chat Test

Login as customer first, then run:

```powershell
Invoke-RestMethod -Uri "$base/conversations" -Headers $headers
Invoke-RestMethod -Uri "$base/conversations/1/messages" -Headers $headers

$message = @{ messageText = "Hello from API test"; attachmentUrl = $null } | ConvertTo-Json
Invoke-RestMethod -Uri "$base/conversations/1/messages" -Method Post -Headers $headers -ContentType "application/json" -Body $message
```

## Payment Placeholder Test

Login as customer first, then run:

```powershell
$payment = @{ requestId = 2; amount = 500 } | ConvertTo-Json
Invoke-RestMethod -Uri "$base/payments/create-checkout-session" -Method Post -Headers $headers -ContentType "application/json" -Body $payment
```

Expected result includes a prototype PayMongo checkout URL.

## Important

Use `https://localhost:5001/api` for logged-in tests. If you use `http://localhost:5000/api`, the API redirects to HTTPS and some clients drop the bearer token.
