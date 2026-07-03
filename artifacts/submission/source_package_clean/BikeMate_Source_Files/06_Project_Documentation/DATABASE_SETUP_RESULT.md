# Database Setup Result

Date: 2026-06-09

## Database

Configured database:

```text
BikeMatesDB_Dev
```

Configured SQL Server:

```text
localhost\SQLEXPRESS
```

Connection string file:

```text
BikeMate.Api\appsettings.Development.json
```

Connection string:

```text
Server=localhost\SQLEXPRESS;Database=BikeMatesDB_Dev;Trusted_Connection=True;TrustServerCertificate=True;
```

## Migration Command

```powershell
dotnet tool run dotnet-ef database update --project .\BikeMate.Infrastructure\BikeMate.Infrastructure.csproj --startup-project .\BikeMate.Api\BikeMate.Api.csproj
```

Result:

```text
Build succeeded.
No migrations were applied. The database is already up to date.
Done.
```

## Tables

Verified base table count: 36.

Important tables are present for:

- users and roles
- OTP and password reset
- device tokens
- customers, addresses, motorcycles
- mechanics and availability
- shops, services, products
- service requests and status history
- live locations
- conversations and messages
- payments and payment events
- reviews, notifications, audit logs

## Seed Users

All seed users exist, are active, and are email verified:

| Email | Password |
| --- | --- |
| `customer@bikemate.test` | `Password123!` |
| `mechanic@bikemate.test` | `Password123!` |
| `shop@bikemate.test` | `Password123!` |
| `admin@bikemate.test` | `Password123!` |

## Lookup Data

Roles verified:

- Customer
- Mechanic
- ShopAdmin
- SystemAdmin

Request statuses verified:

- pending
- accepted
- rejected
- en_route
- arrived
- in_progress
- completed
- cancelled

Payment statuses verified:

- unpaid
- pending
- paid
- failed
- cancelled
- refunded

Payment methods verified:

- gcash
- maya
- card
- grab_pay
- cash
- bank_transfer

## Notes

Final API smoke testing added extra development chat messages and pending PayMongo placeholder payment records. That is normal for local testing.
