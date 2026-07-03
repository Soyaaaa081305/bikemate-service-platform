using BikeMate.Api.Helpers;
using BikeMate.Api.Services;
using BikeMate.Core.Constants;
using BikeMate.Core.Entities;
using BikeMate.Core.DTOs;
using BikeMate.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BikeMate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.SystemAdmin)]
public sealed class AdminController(BikeMateDbContext db, IAdminReportService reports) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<AdminDashboardDto>> Dashboard(CancellationToken cancellationToken)
    {
        return Ok(await reports.GetDashboardAsync(cancellationToken));
    }

    [HttpGet("users")]
    public async Task<IActionResult> Users(CancellationToken cancellationToken)
    {
        return Ok(await db.Users
            .Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.UserId,
                x.FirstName,
                x.LastName,
                x.Email,
                x.PhoneNumber,
                x.AccountStatus,
                x.EmailVerified,
                Roles = x.UserRoles.Select(r => r.Role!.RoleName)
            })
            .ToArrayAsync(cancellationToken));
    }

    [HttpPut("users/{userId:int}/status")]
    public async Task<IActionResult> UpdateUserStatus(int userId, UpdateUserStatusDto dto, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleAsync(x => x.UserId == userId, cancellationToken);
        var oldStatus = user.AccountStatus;
        user.AccountStatus = dto.AccountStatus;
        user.UpdatedAt = DateTime.UtcNow;
        AddAudit("UpdateUserStatus", "users", userId.ToString(), oldStatus, dto.AccountStatus);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "User status updated." });
    }

    [HttpPut("users/{userId:int}/disable")]
    public Task<IActionResult> DisableUser(int userId, CancellationToken cancellationToken)
    {
        return UpdateUserStatus(userId, new UpdateUserStatusDto("disabled"), cancellationToken);
    }

    [HttpPut("users/{userId:int}/enable")]
    public Task<IActionResult> EnableUser(int userId, CancellationToken cancellationToken)
    {
        return UpdateUserStatus(userId, new UpdateUserStatusDto("active"), cancellationToken);
    }

    [HttpDelete("users/{userId:int}")]
    public async Task<IActionResult> DeleteUser(int userId, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .Include(x => x.AuthProviders)
            .Include(x => x.DeviceTokens)
            .SingleAsync(x => x.UserId == userId, cancellationToken);

        if (user.UserRoles.Any(role => string.Equals(role.Role?.RoleName, AppRoles.SystemAdmin, StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest(new { error = "System admin accounts cannot be deleted from the admin user directory." });
        }

        if (string.Equals(user.AccountStatus, "deleted", StringComparison.OrdinalIgnoreCase))
        {
            return NoContent();
        }

        var oldValue = $"{user.Email}|{user.AccountStatus}";
        DeletedAccountIdentity.Anonymize(user);
        AddAudit("DeleteUserAccount", "users", userId.ToString(), oldValue, "deleted");
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("customers")]
    public async Task<IActionResult> Customers(CancellationToken cancellationToken)
    {
        return Ok(await db.Clients
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.ClientId,
                x.UserId,
                FullName = x.User!.FirstName + " " + x.User.LastName,
                x.User.Email,
                x.User.PhoneNumber,
                x.User.AccountStatus,
                x.CreatedAt
            })
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("customers/pending")]
    public async Task<ActionResult<IReadOnlyCollection<CustomerApplicationDto>>> PendingCustomers(CancellationToken cancellationToken)
    {
        return Ok(await db.Clients
            .Include(x => x.User)
            .Include(x => x.Addresses)
            .Where(x => x.User!.AccountStatus == "pending")
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new CustomerApplicationDto(
                x.ClientId,
                x.UserId,
                x.User!.FirstName,
                x.MiddleName,
                x.User.LastName,
                x.User.FirstName + " " + x.User.LastName,
                x.User.Email,
                x.User.PhoneNumber,
                x.User.AccountStatus,
                x.User.EmailVerified,
                x.Sex,
                x.Birthdate,
                x.User.ProfileImageUrl,
                x.ValidIdImageUrl,
                x.Addresses.OrderByDescending(a => a.IsDefault).Select(a => a.AddressLine).FirstOrDefault(),
                x.Addresses.OrderByDescending(a => a.IsDefault).Select(a => a.Barangay).FirstOrDefault(),
                x.Addresses.OrderByDescending(a => a.IsDefault).Select(a => a.City).FirstOrDefault(),
                x.Addresses.OrderByDescending(a => a.IsDefault).Select(a => a.Province).FirstOrDefault(),
                x.Addresses.OrderByDescending(a => a.IsDefault).Select(a => a.PostalCode).FirstOrDefault(),
                x.User.CreatedAt,
                x.User.UpdatedAt))
            .ToArrayAsync(cancellationToken));
    }

    [HttpPut("customers/{clientId:int}/verify")]
    public async Task<IActionResult> VerifyCustomer(int clientId, VerificationDecisionDto dto, CancellationToken cancellationToken)
    {
        var customer = await db.Clients.Include(x => x.User).SingleAsync(x => x.ClientId == clientId, cancellationToken);
        if (customer.User is null)
        {
            return BadRequest(new { error = "This customer does not have a user account to activate." });
        }

        if (!customer.User.EmailVerified)
        {
            return BadRequest(new { error = "The customer must verify the email OTP before approval." });
        }

        if (string.IsNullOrWhiteSpace(customer.ValidIdImageUrl))
        {
            return BadRequest(new { error = "A valid ID image is required before customer approval." });
        }

        customer.User.AccountStatus = "active";
        customer.User.UpdatedAt = DateTime.UtcNow;
        AddAudit("VerifyCustomer", "clients", clientId.ToString(), "pending", "active");
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Customer approved.", dto.Notes });
    }

    [HttpPut("customers/{clientId:int}/reject")]
    public async Task<IActionResult> RejectCustomer(int clientId, VerificationDecisionDto dto, CancellationToken cancellationToken)
    {
        var customer = await db.Clients.Include(x => x.User).SingleAsync(x => x.ClientId == clientId, cancellationToken);
        if (customer.User is not null)
        {
            customer.User.AccountStatus = "rejected";
            customer.User.UpdatedAt = DateTime.UtcNow;
        }

        AddAudit("RejectCustomer", "clients", clientId.ToString(), "pending", dto.Notes ?? "rejected");
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Customer rejected.", dto.Notes });
    }

    [HttpGet("mechanics")]
    public async Task<IActionResult> Mechanics(CancellationToken cancellationToken)
    {
        return Ok(await db.Mechanics
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.MechanicId,
                x.UserId,
                FullName = x.User!.FirstName + " " + x.User.LastName,
                x.User.Email,
                x.IsVerified,
                x.AvailabilityStatus,
                x.AverageRating,
                x.TotalCompletedJobs
            })
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("shops")]
    public async Task<IActionResult> Shops(CancellationToken cancellationToken)
    {
        return Ok(await db.Shops
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.ShopId,
                x.OwnerUserId,
                x.ShopName,
                x.City,
                x.Province,
                x.ShopStatus,
                x.CreatedAt
            })
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("mechanics/pending")]
    public async Task<ActionResult<IReadOnlyCollection<MechanicApplicationDto>>> PendingMechanics(CancellationToken cancellationToken)
    {
        return Ok(await db.Mechanics
            .Include(x => x.User)
            .Include(x => x.ShopMechanics).ThenInclude(x => x.Shop)
            .Where(x =>
                x.User != null &&
                x.User.EmailVerified &&
                x.User.AccountStatus == "pending" &&
                !x.IsVerified)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new MechanicApplicationDto(
                x.MechanicId,
                x.UserId,
                x.User!.FirstName,
                x.MiddleName,
                x.User.LastName,
                x.User.FirstName + " " + x.User.LastName,
                x.User.Email,
                x.User.PhoneNumber,
                x.User.AccountStatus,
                x.User.EmailVerified,
                x.IsVerified,
                x.AvailabilityStatus,
                x.Sex,
                x.Birthdate,
                x.AddressLine,
                x.Barangay,
                x.City,
                x.Province,
                x.ZipCode,
                x.User.ProfileImageUrl,
                x.ValidIdImageUrl,
                x.CertificationImageUrl,
                x.Bio,
                x.YearsExperience,
                x.ShopMechanics.OrderByDescending(sm => sm.AssignedAt).Select(sm => (int?)sm.ShopId).FirstOrDefault(),
                x.ShopMechanics.OrderByDescending(sm => sm.AssignedAt).Select(sm => sm.Shop!.ShopName).FirstOrDefault(),
                x.ShopMechanics.Any(sm => sm.IsActive),
                x.CreatedAt,
                x.UpdatedAt))
            .ToArrayAsync(cancellationToken));
    }

    [HttpPut("mechanics/{mechanicId:int}/verify")]
    public async Task<IActionResult> VerifyMechanic(int mechanicId, VerificationDecisionDto dto, CancellationToken cancellationToken)
    {
        var mechanic = await db.Mechanics
            .Include(x => x.User)
            .Include(x => x.ShopMechanics)
            .SingleAsync(x => x.MechanicId == mechanicId, cancellationToken);
        if (mechanic.User is null)
        {
            return BadRequest(new { error = "This mechanic does not have a user account to activate." });
        }

        if (!mechanic.User.EmailVerified)
        {
            return BadRequest(new { error = "The mechanic must verify the email OTP before approval." });
        }

        if (string.IsNullOrWhiteSpace(mechanic.ValidIdImageUrl) || string.IsNullOrWhiteSpace(mechanic.CertificationImageUrl))
        {
            return BadRequest(new { error = "Valid ID and mechanic certification/license files are required before approval." });
        }

        mechanic.IsVerified = true;
        mechanic.User.AccountStatus = "active";
        mechanic.UpdatedAt = DateTime.UtcNow;
        mechanic.User.UpdatedAt = DateTime.UtcNow;
        foreach (var assignment in mechanic.ShopMechanics)
        {
            assignment.IsActive = true;
        }

        AddAudit("VerifyMechanic", "mechanics", mechanicId.ToString(), "pending", "verified");
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Mechanic verified.", dto.Notes });
    }

    [HttpPut("mechanics/{mechanicId:int}/reject")]
    public async Task<IActionResult> RejectMechanic(int mechanicId, VerificationDecisionDto dto, CancellationToken cancellationToken)
    {
        var mechanic = await db.Mechanics.Include(x => x.User).Include(x => x.ShopMechanics).SingleAsync(x => x.MechanicId == mechanicId, cancellationToken);
        mechanic.IsVerified = false;
        mechanic.User!.AccountStatus = "rejected";
        mechanic.UpdatedAt = DateTime.UtcNow;
        mechanic.User.UpdatedAt = DateTime.UtcNow;
        foreach (var assignment in mechanic.ShopMechanics)
        {
            assignment.IsActive = false;
        }

        AddAudit("RejectMechanic", "mechanics", mechanicId.ToString(), "pending", dto.Notes ?? "rejected");
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Mechanic rejected.", dto.Notes });
    }

    [HttpGet("shops/pending")]
    public async Task<IActionResult> PendingShops(CancellationToken cancellationToken)
    {
        return Ok(await db.Shops
            .Include(x => x.Owner)
            .Where(x => x.ShopStatus == "pending")
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.ShopId,
                x.ShopName,
                x.ShopDescription,
                x.AddressLine,
                x.City,
                x.Province,
                x.ContactNumber,
                x.BusinessPermitUrl,
                x.ShopImageUrl,
                x.OwnerValidIdUrl,
                x.OwnerMiddleName,
                x.OwnerSex,
                x.OwnerBirthdate,
                x.OwnerAddressLine,
                x.OwnerBarangay,
                x.OwnerCity,
                x.OwnerProvince,
                x.OwnerZipCode,
                x.ShopStatus,
                x.CreatedAt,
                Owner = x.Owner == null ? null : new
                {
                    x.Owner.UserId,
                    x.Owner.FirstName,
                    x.Owner.LastName,
                    x.Owner.Email,
                    x.Owner.PhoneNumber,
                    x.Owner.EmailVerified,
                    x.Owner.AccountStatus
                }
            })
            .ToArrayAsync(cancellationToken));
    }

    [HttpPut("shops/{shopId:int}/verify")]
    public async Task<IActionResult> VerifyShop(int shopId, VerificationDecisionDto dto, CancellationToken cancellationToken)
    {
        var shop = await db.Shops
            .Include(x => x.Owner)
            .SingleAsync(x => x.ShopId == shopId, cancellationToken);
        if (shop.Owner is null)
        {
            return BadRequest(new { error = "This shop does not have an owner account to activate." });
        }

        if (!shop.Owner.EmailVerified)
        {
            return BadRequest(new { error = "The owner must verify the email OTP before this shop can be approved." });
        }

        shop.ShopStatus = "verified";
        shop.UpdatedAt = DateTime.UtcNow;
        shop.Owner.AccountStatus = "active";
        shop.Owner.UpdatedAt = DateTime.UtcNow;
        AddAudit("VerifyShop", "shops", shopId.ToString(), "pending", "verified");
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Shop verified.", dto.Notes });
    }

    [HttpPut("shops/{shopId:int}/reject")]
    public async Task<IActionResult> RejectShop(int shopId, VerificationDecisionDto dto, CancellationToken cancellationToken)
    {
        var shop = await db.Shops
            .Include(x => x.Owner)
            .SingleAsync(x => x.ShopId == shopId, cancellationToken);
        shop.ShopStatus = "rejected";
        shop.UpdatedAt = DateTime.UtcNow;
        if (shop.Owner is not null)
        {
            shop.Owner.AccountStatus = "rejected";
            shop.Owner.UpdatedAt = DateTime.UtcNow;
        }

        AddAudit("RejectShop", "shops", shopId.ToString(), "pending", dto.Notes ?? "rejected");
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Shop rejected.", dto.Notes });
    }

    [HttpGet("service-requests")]
    public async Task<IActionResult> ServiceRequests(CancellationToken cancellationToken)
    {
        return Ok(await db.ServiceRequests
            .Include(x => x.CurrentStatus)
            .Include(x => x.Client).ThenInclude(x => x!.User)
            .Include(x => x.Mechanic).ThenInclude(x => x!.User)
            .Include(x => x.Shop)
            .Include(x => x.ShopService)
            .OrderByDescending(x => x.CreatedAt)
            .Select(ServiceRequestService.ToDtoExpression())
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("emergency-requests")]
    public async Task<IActionResult> EmergencyRequests(CancellationToken cancellationToken)
    {
        return Ok(await db.ServiceRequests
            .Include(x => x.CurrentStatus)
            .Include(x => x.Client).ThenInclude(x => x!.User)
            .Include(x => x.Mechanic).ThenInclude(x => x!.User)
            .Include(x => x.Shop)
            .Include(x => x.ShopService)
            .Where(x => x.IssueDescription.StartsWith("[EMERGENCY]"))
            .OrderByDescending(x => x.CreatedAt)
            .Select(ServiceRequestService.ToDtoExpression())
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("payments")]
    public async Task<IActionResult> Payments(CancellationToken cancellationToken)
    {
        return Ok(await db.Payments
            .Include(x => x.PaymentStatus)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.ToDto())
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("reports/revenue")]
    public async Task<ActionResult<RevenueReportDto>> Revenue([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken)
    {
        return Ok(await reports.GetRevenueAsync(from ?? DateTime.UtcNow.AddMonths(-1), to ?? DateTime.UtcNow, cancellationToken));
    }

    [HttpGet("reports/top-services")]
    public async Task<ActionResult<IReadOnlyCollection<TopServiceDto>>> TopServices(CancellationToken cancellationToken)
    {
        return Ok(await reports.GetTopServicesAsync(cancellationToken));
    }

    [HttpGet("reports/top-mechanics")]
    public async Task<ActionResult<IReadOnlyCollection<TopMechanicDto>>> TopMechanics(CancellationToken cancellationToken)
    {
        return Ok(await reports.GetTopMechanicsAsync(cancellationToken));
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> AuditLogs(CancellationToken cancellationToken)
    {
        return Ok(await db.AuditLogs
            .Include(x => x.ActorUser)
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .Select(x => new
            {
                x.AuditId,
                Actor = x.ActorUser == null ? null : x.ActorUser.FirstName + " " + x.ActorUser.LastName,
                x.ActionName,
                x.EntityName,
                x.EntityId,
                x.OldValuesJson,
                x.NewValuesJson,
                x.CreatedAt
            })
            .ToArrayAsync(cancellationToken));
    }

    [HttpPost("announcements")]
    public async Task<IActionResult> Announcement(AdminAnnouncementDto dto, CancellationToken cancellationToken)
    {
        var users = await db.Users.Where(x => x.AccountStatus == "active").Select(x => x.UserId).ToArrayAsync(cancellationToken);
        foreach (var userId in users)
        {
            db.Notifications.Add(new Notification
            {
                UserId = userId,
                NotificationType = "announcement",
                Title = dto.Title,
                Message = dto.Message,
                CreatedAt = DateTime.UtcNow
            });
        }

        AddAudit("CreateAnnouncement", "notifications", null, null, dto.Title);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Announcement sent.", recipients = users.Length });
    }

    private void AddAudit(string action, string entity, string? entityId, string? oldValue, string? newValue)
    {
        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = User.GetUserId(),
            ActionName = action,
            EntityName = entity,
            EntityId = entityId,
            OldValuesJson = oldValue,
            NewValuesJson = newValue,
            CreatedAt = DateTime.UtcNow
        });
    }
}
