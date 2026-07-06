using BikeMate.Api.Helpers;
using BikeMate.Api.Services;
using BikeMate.Core.Constants;
using BikeMate.Core.DTOs;
using BikeMate.Core.Entities;
using BikeMate.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BikeMate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.Customer)]
public sealed class CustomersController(BikeMateDbContext db) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<CustomerMeDto>> GetMe(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var customer = await db.Clients
            .Include(x => x.User)
            .Include(x => x.Addresses)
            .Include(x => x.Motorcycles)
            .SingleAsync(x => x.UserId == userId, cancellationToken);

        return Ok(new CustomerMeDto(
            customer.ClientId,
            customer.UserId,
            customer.User!.FirstName,
            customer.MiddleName,
            customer.User.LastName,
            customer.User.Email,
            customer.User.PhoneNumber,
            customer.User.ProfileImageUrl,
            customer.User.EmailVerified,
            customer.User.PhoneVerified,
            customer.User.AccountStatus,
            customer.User.CreatedAt,
            customer.User.UpdatedAt,
            customer.Sex,
            customer.Birthdate,
            customer.ValidIdImageUrl,
            customer.Addresses.Select(ToAddressDto).ToArray(),
            customer.Motorcycles.Select(ToMotorcycleDto).ToArray()));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(UpsertCustomerProfileDto dto, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var customer = await db.Clients.Include(x => x.User).SingleAsync(x => x.UserId == userId, cancellationToken);
        var user = customer.User!;
        var email = AuthService.NormalizeEmail(dto.Email);
        var phone = AuthService.NormalizePhilippineMobile(dto.PhoneNumber);

        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase) &&
            await db.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        if (!string.IsNullOrWhiteSpace(phone) &&
            !string.Equals(user.PhoneNumber, phone, StringComparison.OrdinalIgnoreCase) &&
            await db.Users.AnyAsync(x => x.PhoneNumber == phone, cancellationToken))
        {
            throw new InvalidOperationException("Phone number is already registered.");
        }

        if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
        {
            throw new InvalidOperationException("First name and last name are required.");
        }

        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();
        user.Email = email;
        user.PhoneNumber = phone;
        user.UpdatedAt = DateTime.UtcNow;

        customer.MiddleName = Clean(dto.MiddleName);
        customer.Sex = Clean(dto.Sex);
        customer.Birthdate = dto.Birthdate is null
            ? null
            : AgeRequirement.RequireAdult(dto.Birthdate, "Customer");

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Profile updated." });
    }

    [HttpPut("me/profile-image")]
    public async Task<IActionResult> UpdateProfileImage(UploadMediaDto dto, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleAsync(x => x.UserId == User.GetUserId(), cancellationToken);
        user.ProfileImageUrl = RequireMediaUrl(dto.MediaUrl, "Profile photo");
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Profile photo updated." });
    }

    [HttpPut("me/valid-id")]
    public async Task<IActionResult> UpdateValidId(UploadMediaDto dto, CancellationToken cancellationToken)
    {
        var customer = await db.Clients
            .Include(x => x.User)
            .SingleAsync(x => x.UserId == User.GetUserId(), cancellationToken);
        customer.ValidIdImageUrl = RequireMediaUrl(dto.MediaUrl, "Valid ID");
        customer.User!.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Valid ID updated." });
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMe(CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(x => x.DeviceTokens)
            .Include(x => x.AuthProviders)
            .SingleAsync(x => x.UserId == User.GetUserId(), cancellationToken);

        DeletedAccountIdentity.Anonymize(user);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var customer = await db.Clients
            .Include(x => x.User)
            .Include(x => x.Addresses)
            .Include(x => x.Motorcycles)
            .SingleAsync(x => x.UserId == userId, cancellationToken);

        var activeStatuses = new[] { "pending", "emergency_pending", "searching_responder", "accepted", "en_route", "arrived", "in_progress", "call_connecting" };
        var activeBooking = await db.ServiceRequests
            .Include(x => x.CurrentStatus)
            .Include(x => x.Client).ThenInclude(x => x!.User)
            .Include(x => x.Mechanic).ThenInclude(x => x!.User)
            .Include(x => x.Shop)
            .Include(x => x.ShopService)
            .Where(x => x.ClientId == customer.ClientId && activeStatuses.Contains(x.CurrentStatus!.StatusName))
            .OrderByDescending(x => x.CreatedAt)
            .Select(ServiceRequestService.ToDtoExpression())
            .FirstOrDefaultAsync(cancellationToken);

        var upcoming = await db.ServiceRequests
            .Include(x => x.CurrentStatus)
            .Include(x => x.Client).ThenInclude(x => x!.User)
            .Include(x => x.Mechanic).ThenInclude(x => x!.User)
            .Include(x => x.Shop)
            .Include(x => x.ShopService)
            .Where(x => x.ClientId == customer.ClientId &&
                        x.ScheduledAt >= DateTime.UtcNow &&
                        x.CurrentStatus!.StatusName != "completed" &&
                        x.CurrentStatus.StatusName != "cancelled")
            .OrderBy(x => x.ScheduledAt)
            .Select(ServiceRequestService.ToDtoExpression())
            .FirstOrDefaultAsync(cancellationToken);

        var recentHistory = await db.ServiceRequests
            .Include(x => x.CurrentStatus)
            .Include(x => x.Client).ThenInclude(x => x!.User)
            .Include(x => x.Mechanic).ThenInclude(x => x!.User)
            .Include(x => x.Shop)
            .Include(x => x.ShopService)
            .Where(x => x.ClientId == customer.ClientId &&
                        (x.CurrentStatus!.StatusName == "completed" ||
                         x.CurrentStatus.StatusName == "cancelled" ||
                         x.CurrentStatus.StatusName == "rejected"))
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .Select(ServiceRequestService.ToDtoExpression())
            .ToArrayAsync(cancellationToken);

        var categories = await db.ServiceCategories
            .Where(x => x.IsActive)
            .OrderBy(x => x.CategoryName)
            .Select(x => new ServiceCategoryDto(x.CategoryId, x.CategoryName, x.Description))
            .ToArrayAsync(cancellationToken);
        var shops = await db.Shops
            .Where(x => x.ShopStatus == "verified")
            .OrderBy(x => x.ShopName)
            .Take(5)
            .Select(x => new ShopSummaryDto(x.ShopId, x.ShopName, x.AddressLine, x.City, x.ContactNumber, x.ShopStatus, x.Latitude, x.Longitude, x.ShopImageUrl, x.ShopLogoUrl, 0, null, x.AllowsReservations, x.AllowsPickup, x.AllowsOnsiteRepair))
            .ToArrayAsync(cancellationToken);
        var mechanics = await db.Mechanics
            .Include(x => x.User)
            .Where(x => x.AvailabilityStatus != "offline")
            .OrderByDescending(x => x.AverageRating)
            .Take(5)
            .Select(x => new MechanicProfileDto(x.MechanicId, x.User!.FirstName + " " + x.User.LastName, x.User.ProfileImageUrl, x.Bio, x.YearsExperience, x.IsVerified, x.AvailabilityStatus, x.AverageRating, x.TotalCompletedJobs))
            .ToArrayAsync(cancellationToken);

        return Ok(new
        {
            Profile = new
            {
                customer.ClientId,
                customer.UserId,
                customer.User!.FirstName,
                customer.User.LastName,
                customer.User.Email,
                customer.User.PhoneNumber,
                customer.User.ProfileImageUrl
            },
            DefaultAddress = customer.Addresses.OrderByDescending(x => x.IsDefault).Select(ToAddressDto).FirstOrDefault(),
            Motorcycles = customer.Motorcycles.Select(ToMotorcycleDto),
            ActiveBooking = activeBooking,
            UpcomingBooking = upcoming,
            ServiceCategories = categories,
            NearbyMechanics = mechanics,
            NearbyShops = shops,
            RecentHistory = recentHistory,
            UnreadNotifications = await db.Notifications.CountAsync(x => x.UserId == userId && !x.IsRead, cancellationToken),
            UnreadMessages = await db.ConversationParticipants.CountAsync(x => x.UserId == userId && x.LastReadAt == null, cancellationToken)
        });
    }

    [HttpGet("home-status")]
    public async Task<IActionResult> HomeStatus(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var clientId = await GetClientIdAsync(cancellationToken);
        return Ok(new
        {
            ActiveBookings = await db.ServiceRequests.CountAsync(x =>
                x.ClientId == clientId &&
                x.CurrentStatus!.StatusName != "completed" &&
                x.CurrentStatus.StatusName != "cancelled" &&
                x.CurrentStatus.StatusName != "rejected", cancellationToken),
            UpcomingSchedules = await db.ServiceRequests.CountAsync(x =>
                x.ClientId == clientId &&
                x.ScheduledAt >= DateTime.UtcNow &&
                x.CurrentStatus!.StatusName != "completed" &&
                x.CurrentStatus.StatusName != "cancelled", cancellationToken),
            UnreadNotifications = await db.Notifications.CountAsync(x => x.UserId == userId && !x.IsRead, cancellationToken),
            UnreadMessages = await db.ConversationParticipants.CountAsync(x => x.UserId == userId && x.LastReadAt == null, cancellationToken)
        });
    }

    [HttpGet("address")]
    public async Task<ActionResult<IReadOnlyCollection<CustomerAddressDto>>> GetAddresses(CancellationToken cancellationToken)
    {
        var clientId = await GetClientIdAsync(cancellationToken);
        return Ok(await db.ClientAddresses
            .Where(x => x.ClientId == clientId)
            .Select(x => ToAddressDto(x))
            .ToArrayAsync(cancellationToken));
    }

    [HttpPost("address")]
    public async Task<ActionResult<CustomerAddressDto>> AddAddress(UpsertCustomerAddressDto dto, CancellationToken cancellationToken)
    {
        var clientId = await GetClientIdAsync(cancellationToken);
        if (dto.IsDefault)
        {
            await db.ClientAddresses.Where(x => x.ClientId == clientId).ExecuteUpdateAsync(x => x.SetProperty(a => a.IsDefault, false), cancellationToken);
        }

        var address = new ClientAddress
        {
            ClientId = clientId,
            Label = Clean(dto.Label) ?? "Home",
            AddressLine = Required(dto.AddressLine, "Address"),
            Barangay = Clean(dto.Barangay),
            City = Clean(dto.City),
            Province = Clean(dto.Province),
            PostalCode = Clean(dto.PostalCode),
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            IsDefault = dto.IsDefault,
            CreatedAt = DateTime.UtcNow
        };
        db.ClientAddresses.Add(address);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToAddressDto(address));
    }

    [HttpPut("address/{id:int}")]
    public async Task<ActionResult<CustomerAddressDto>> UpdateAddress(int id, UpsertCustomerAddressDto dto, CancellationToken cancellationToken)
    {
        var clientId = await GetClientIdAsync(cancellationToken);
        var address = await db.ClientAddresses.SingleAsync(x => x.AddressId == id && x.ClientId == clientId, cancellationToken);
        if (dto.IsDefault)
        {
            await db.ClientAddresses
                .Where(x => x.ClientId == clientId && x.AddressId != id)
                .ExecuteUpdateAsync(x => x.SetProperty(a => a.IsDefault, false), cancellationToken);
        }

        address.Label = Clean(dto.Label) ?? "Home";
        address.AddressLine = Required(dto.AddressLine, "Address");
        address.Barangay = Clean(dto.Barangay);
        address.City = Clean(dto.City);
        address.Province = Clean(dto.Province);
        address.PostalCode = Clean(dto.PostalCode);
        address.Latitude = dto.Latitude;
        address.Longitude = dto.Longitude;
        address.IsDefault = dto.IsDefault;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToAddressDto(address));
    }

    [HttpGet("motorcycles")]
    public async Task<ActionResult<IReadOnlyCollection<MotorcycleDto>>> GetMotorcycles(CancellationToken cancellationToken)
    {
        var clientId = await GetClientIdAsync(cancellationToken);
        return Ok(await db.Motorcycles.Where(x => x.ClientId == clientId).Select(x => ToMotorcycleDto(x)).ToArrayAsync(cancellationToken));
    }

    [HttpPost("motorcycles")]
    public async Task<ActionResult<MotorcycleDto>> AddMotorcycle(UpsertMotorcycleDto dto, CancellationToken cancellationToken)
    {
        var motorcycle = new Motorcycle
        {
            ClientId = await GetClientIdAsync(cancellationToken),
            Brand = dto.Brand,
            Model = dto.Model,
            YearModel = dto.YearModel,
            PlateNumber = dto.PlateNumber,
            EngineType = dto.EngineType,
            Color = dto.Color,
            MotorcycleImageUrl = dto.MotorcycleImageUrl,
            CreatedAt = DateTime.UtcNow
        };
        db.Motorcycles.Add(motorcycle);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToMotorcycleDto(motorcycle));
    }

    [HttpPut("motorcycles/{id:int}")]
    public async Task<ActionResult<MotorcycleDto>> UpdateMotorcycle(int id, UpsertMotorcycleDto dto, CancellationToken cancellationToken)
    {
        var clientId = await GetClientIdAsync(cancellationToken);
        var motorcycle = await db.Motorcycles.SingleAsync(x => x.MotorcycleId == id && x.ClientId == clientId, cancellationToken);
        motorcycle.Brand = dto.Brand;
        motorcycle.Model = dto.Model;
        motorcycle.YearModel = dto.YearModel;
        motorcycle.PlateNumber = dto.PlateNumber;
        motorcycle.EngineType = dto.EngineType;
        motorcycle.Color = dto.Color;
        motorcycle.MotorcycleImageUrl = dto.MotorcycleImageUrl;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToMotorcycleDto(motorcycle));
    }

    [HttpDelete("motorcycles/{id:int}")]
    public async Task<IActionResult> DeleteMotorcycle(int id, CancellationToken cancellationToken)
    {
        var clientId = await GetClientIdAsync(cancellationToken);
        var motorcycle = await db.Motorcycles.SingleAsync(x => x.MotorcycleId == id && x.ClientId == clientId, cancellationToken);
        db.Motorcycles.Remove(motorcycle);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<int> GetClientIdAsync(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        return await db.Clients.Where(x => x.UserId == userId).Select(x => x.ClientId).SingleAsync(cancellationToken);
    }

    private static CustomerAddressDto ToAddressDto(ClientAddress x)
    {
        return new CustomerAddressDto(x.AddressId, x.Label, x.AddressLine, x.Barangay, x.City, x.Province, x.PostalCode, x.Latitude, x.Longitude, x.IsDefault);
    }

    private static MotorcycleDto ToMotorcycleDto(Motorcycle x)
    {
        return new MotorcycleDto(x.MotorcycleId, x.Brand, x.Model, x.YearModel, x.PlateNumber, x.EngineType, x.Color, x.MotorcycleImageUrl);
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string Required(string? value, string label)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"{label} is required.")
            : value.Trim();
    }

    private static string RequireMediaUrl(string? value, string label)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"{label} file is required.")
            : value.Trim();
    }
}
