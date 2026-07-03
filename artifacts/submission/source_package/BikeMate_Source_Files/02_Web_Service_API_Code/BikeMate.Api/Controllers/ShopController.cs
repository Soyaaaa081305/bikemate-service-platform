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
[Route("api/shop")]
[Authorize(Roles = $"{AppRoles.ShopAdmin},{AppRoles.SystemAdmin}")]
public sealed class ShopController(
    BikeMateDbContext db,
    IBookingConversationService bookingConversationService,
    IPasswordService passwordService,
    IOtpService otpService,
    IEmailService emailService) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        var paidStatusId = await db.PaymentStatuses.Where(x => x.StatusName == "paid").Select(x => x.PaymentStatusId).SingleAsync(cancellationToken);
        var activeStatuses = new[] { "pending", "accepted", "en_route", "arrived", "in_progress", "emergency_pending" };

        return Ok(new
        {
            Profile = ToDetails(shop),
            ActiveBookings = await db.ServiceRequests.CountAsync(x => x.ShopId == shop.ShopId && activeStatuses.Contains(x.CurrentStatus!.StatusName), cancellationToken),
            TodaysBookings = await db.ServiceRequests.CountAsync(x => x.ShopId == shop.ShopId && x.CreatedAt.Date == DateTime.UtcNow.Date, cancellationToken),
            MonthlyRevenue = await db.Payments.Where(x => x.PaymentStatusId == paidStatusId && x.Request!.ShopId == shop.ShopId && x.CreatedAt >= DateTime.UtcNow.AddDays(-30)).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m,
            Services = await db.ShopServices.CountAsync(x => x.ShopId == shop.ShopId && x.IsActive, cancellationToken),
            InventoryAlerts = await db.Products.CountAsync(x => x.ShopId == shop.ShopId && x.IsActive && x.StockQuantity <= 5, cancellationToken),
            Mechanics = await db.ShopMechanics.CountAsync(x => x.ShopId == shop.ShopId && x.IsActive, cancellationToken),
            AverageRating = await db.ShopMechanics
                .Where(x => x.ShopId == shop.ShopId && x.IsActive)
                .Select(x => (decimal?)x.Mechanic!.AverageRating)
                .AverageAsync(cancellationToken) ?? 0m
        });
    }

    [HttpGet("profile")]
    public async Task<ActionResult<ShopDetailsDto>> Profile(CancellationToken cancellationToken)
    {
        return Ok(ToDetails(await GetOwnedShopAsync(cancellationToken)));
    }

    [HttpGet("application")]
    public async Task<ActionResult<ShopApplicationDetailsDto>> ApplicationDetails(CancellationToken cancellationToken)
    {
        return Ok(ToApplicationDetails(await GetOwnedShopWithOwnerAsync(cancellationToken)));
    }

    [HttpPut("profile")]
    public async Task<ActionResult<ShopDetailsDto>> UpdateProfile(UpsertShopDto dto, CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        var existingDtiRegistration = ExtractDtiRegistration(shop.ShopDescription);
        shop.ShopName = dto.ShopName;
        shop.ShopDescription = BuildShopDescription(dto.ShopDescription, existingDtiRegistration);
        shop.AddressLine = dto.AddressLine;
        shop.City = dto.City;
        shop.Province = dto.Province;
        shop.Latitude = dto.Latitude;
        shop.Longitude = dto.Longitude;
        shop.ContactNumber = dto.ContactNumber;
        shop.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToDetails(shop));
    }

    [HttpPost("profile/image")]
    public async Task<IActionResult> ProfileImage(UploadMediaDto dto, CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        shop.ShopImageUrl = dto.MediaUrl;
        shop.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { shop.ShopImageUrl });
    }

    [HttpPost("profile/logo")]
    public async Task<IActionResult> ProfileLogo(UploadMediaDto dto, CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        shop.ShopLogoUrl = dto.MediaUrl;
        shop.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { shop.ShopLogoUrl });
    }

    [HttpGet("services")]
    public async Task<ActionResult<IReadOnlyCollection<ShopServiceDto>>> Services(CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        return Ok(await QueryServices(shop.ShopId, activeOnly: true).ToArrayAsync(cancellationToken));
    }

    [HttpGet("services/{id:int}")]
    public async Task<ActionResult<ShopServiceDto>> Service(int id, CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        var service = await QueryServices(shop.ShopId, activeOnly: true, serviceId: id)
            .SingleOrDefaultAsync(cancellationToken);

        return service is null
            ? NotFound(new { message = "Shop service was not found or has been deactivated." })
            : Ok(service);
    }

    [HttpPost("services")]
    public async Task<ActionResult<ShopServiceDto>> AddService(UpsertShopServiceDto dto, CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        var service = new ShopService
        {
            ShopId = shop.ShopId,
            CategoryId = dto.CategoryId,
            ServiceName = dto.ServiceName,
            ServiceDescription = dto.ServiceDescription,
            BasePrice = dto.BasePrice,
            EstimatedMinutes = dto.EstimatedMinutes,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };
        db.ShopServices.Add(service);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(await QueryServices(shop.ShopId, serviceId: service.ShopServiceId).SingleAsync(cancellationToken));
    }

    [HttpPut("services/{id:int}")]
    public async Task<ActionResult<ShopServiceDto>> UpdateService(int id, UpsertShopServiceDto dto, CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        var service = await db.ShopServices.SingleOrDefaultAsync(x => x.ShopServiceId == id && x.ShopId == shop.ShopId && x.IsActive, cancellationToken);
        if (service is null)
        {
            return NotFound(new { message = "Shop service was not found or has been deactivated." });
        }

        service.CategoryId = dto.CategoryId;
        service.ServiceName = dto.ServiceName;
        service.ServiceDescription = dto.ServiceDescription;
        service.BasePrice = dto.BasePrice;
        service.EstimatedMinutes = dto.EstimatedMinutes;
        service.IsActive = dto.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(await QueryServices(shop.ShopId, serviceId: id).SingleAsync(cancellationToken));
    }

    [HttpDelete("services/{id:int}")]
    public async Task<IActionResult> DeleteService(int id, CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        var service = await db.ShopServices.SingleOrDefaultAsync(x => x.ShopServiceId == id && x.ShopId == shop.ShopId && x.IsActive, cancellationToken);
        if (service is null)
        {
            return NotFound(new { message = "Shop service was not found or has already been deactivated." });
        }

        service.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("bookings")]
    public async Task<ActionResult<IReadOnlyCollection<ServiceRequestDto>>> Bookings(CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        return Ok(await QueryBookings(shop.ShopId).ToArrayAsync(cancellationToken));
    }

    [HttpGet("bookings/{id:int}")]
    public async Task<ActionResult<ServiceRequestDto>> Booking(int id, CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        return Ok(await QueryBookings(shop.ShopId).SingleAsync(x => x.RequestId == id, cancellationToken));
    }

    [HttpPut("bookings/{id:int}/assign-mechanic")]
    public async Task<IActionResult> AssignMechanic(int id, AssignMechanicDto dto, CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        var mechanicBelongsToShop = await db.ShopMechanics.AnyAsync(x => x.ShopId == shop.ShopId && x.MechanicId == dto.MechanicId && x.IsActive, cancellationToken);
        if (!mechanicBelongsToShop)
        {
            return BadRequest(new { message = "Mechanic is not assigned to this shop." });
        }

        var request = await db.ServiceRequests.SingleAsync(x => x.RequestId == id && x.ShopId == shop.ShopId, cancellationToken);
        request.MechanicId = dto.MechanicId;
        await db.SaveChangesAsync(cancellationToken);
        await bookingConversationService.SyncRequestAsync(id, cancellationToken);
        return Ok(new { message = "Mechanic assigned." });
    }

    [HttpGet("mechanics")]
    public async Task<IActionResult> Mechanics(CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        return Ok(await db.ShopMechanics
            .Include(x => x.Mechanic).ThenInclude(x => x!.User)
            .Where(x => x.ShopId == shop.ShopId && x.IsActive && x.Mechanic!.IsVerified && x.Mechanic.User!.AccountStatus == "active")
            .Select(x => new MechanicProfileDto(x.MechanicId, x.Mechanic!.User!.FirstName + " " + x.Mechanic.User.LastName, x.Mechanic.User.ProfileImageUrl, x.Mechanic.Bio, x.Mechanic.YearsExperience, x.Mechanic.IsVerified, x.Mechanic.AvailabilityStatus, x.Mechanic.AverageRating, x.Mechanic.TotalCompletedJobs))
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("mechanic-applications")]
    public async Task<ActionResult<IReadOnlyCollection<MechanicApplicationDto>>> MechanicApplications(CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        return Ok(await QueryMechanicApplications(shop.ShopId).ToArrayAsync(cancellationToken));
    }

    [HttpPost("mechanic-applications")]
    public async Task<ActionResult<MechanicApplicationDto>> CreateMechanicApplication(CreateMechanicApplicationDto dto, CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        var firstName = Require(dto.FirstName, "First name");
        var lastName = Require(dto.LastName, "Last name");
        var email = AuthService.NormalizeEmail(dto.Email);
        var phoneNumber = AuthService.NormalizePhilippineMobile(Require(dto.PhoneNumber, "Phone number"))
            ?? throw new InvalidOperationException("Phone number is required.");
        var password = Require(dto.Password, "Password");
        var birthdate = AgeRequirement.RequireAdult(dto.Birthdate, "Mechanic");
        var validIdImageUrl = Require(dto.ValidIdImageUrl, "Valid ID");
        var certificationImageUrl = Require(dto.CertificationImageUrl, "Mechanic certification or license");

        if (password.Length <= 8)
        {
            throw new InvalidOperationException("Password must be more than 8 characters.");
        }

        if (dto.YearsExperience is < 0 or > 80)
        {
            throw new InvalidOperationException("Years of experience must be between 0 and 80.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        if (await db.Users.AnyAsync(x => x.Email == email && x.AccountStatus != "deleted", cancellationToken))
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        if (await db.Users.AnyAsync(x => x.PhoneNumber == phoneNumber && x.AccountStatus != "deleted", cancellationToken))
        {
            throw new InvalidOperationException("Phone number is already registered.");
        }

        var mechanicRoleId = await db.Roles
            .Where(x => x.RoleName == AppRoles.Mechanic)
            .Select(x => x.RoleId)
            .SingleAsync(cancellationToken);

        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PhoneNumber = phoneNumber,
            PasswordHash = passwordService.HashPassword(password),
            ProfileImageUrl = CleanOptional(dto.ProfileImageUrl),
            EmailVerified = false,
            AccountStatus = "pending",
            CreatedAt = DateTime.UtcNow,
            UserRoles =
            [
                new UserRole
                {
                    RoleId = mechanicRoleId,
                    AssignedAt = DateTime.UtcNow
                }
            ]
        };

        var mechanic = new Mechanic
        {
            User = user,
            MiddleName = CleanOptional(dto.MiddleName),
            Sex = CleanOptional(dto.Sex),
            Birthdate = birthdate,
            AddressLine = CleanOptional(dto.AddressLine),
            Barangay = CleanOptional(dto.Barangay),
            City = CleanOptional(dto.City),
            Province = CleanOptional(dto.Province),
            ZipCode = CleanOptional(dto.ZipCode),
            ValidIdImageUrl = validIdImageUrl,
            CertificationImageUrl = certificationImageUrl,
            Bio = CleanOptional(dto.Bio),
            YearsExperience = dto.YearsExperience,
            IsVerified = false,
            AvailabilityStatus = "offline",
            CreatedAt = DateTime.UtcNow
        };

        db.Mechanics.Add(mechanic);
        await db.SaveChangesAsync(cancellationToken);

        db.ShopMechanics.Add(new ShopMechanic
        {
            ShopId = shop.ShopId,
            MechanicId = mechanic.MechanicId,
            IsActive = false,
            AssignedAt = DateTime.UtcNow
        });

        var otpCode = otpService.GenerateCode();
        db.OtpVerifications.Add(new OtpVerification
        {
            UserId = user.UserId,
            OtpHash = otpService.HashCode(otpCode),
            Purpose = "email_verification",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        await emailService.SendOtpAsync(user, otpCode, "mechanic_email_verification", cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(await QueryMechanicApplications(shop.ShopId, mechanic.MechanicId).SingleAsync(cancellationToken));
    }

    [HttpPost("mechanics")]
    public async Task<IActionResult> AddMechanic(AssignMechanicDto dto, CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        var mechanicReady = await db.Mechanics.AnyAsync(x => x.MechanicId == dto.MechanicId && x.IsVerified && x.User!.AccountStatus == "active", cancellationToken);
        if (!mechanicReady)
        {
            return BadRequest(new { message = "Mechanic must be approved by BikeMate admin before being assigned to the shop." });
        }

        var exists = await db.ShopMechanics.AnyAsync(x => x.ShopId == shop.ShopId && x.MechanicId == dto.MechanicId, cancellationToken);
        if (!exists)
        {
            db.ShopMechanics.Add(new ShopMechanic { ShopId = shop.ShopId, MechanicId = dto.MechanicId, IsActive = true, AssignedAt = DateTime.UtcNow });
        }
        else
        {
            await db.ShopMechanics.Where(x => x.ShopId == shop.ShopId && x.MechanicId == dto.MechanicId).ExecuteUpdateAsync(x => x.SetProperty(sm => sm.IsActive, true), cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Mechanic assigned to shop." });
    }

    [HttpDelete("mechanics/{mechanicId:int}")]
    public async Task<IActionResult> RemoveMechanic(int mechanicId, CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        await db.ShopMechanics
            .Where(x => x.ShopId == shop.ShopId && x.MechanicId == mechanicId)
            .ExecuteUpdateAsync(x => x.SetProperty(sm => sm.IsActive, false), cancellationToken);
        return NoContent();
    }

    [HttpGet("inventory")]
    public async Task<ActionResult<IReadOnlyCollection<ProductDto>>> Inventory(CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        return Ok(await db.Products
            .Include(x => x.Images)
            .Where(x => x.ShopId == shop.ShopId && x.IsActive)
            .Select(ToProductDtoExpression())
            .ToArrayAsync(cancellationToken));
    }

    [HttpPost("inventory")]
    public async Task<ActionResult<ProductDto>> AddInventory(UpsertProductDto dto, CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        var product = new Product
        {
            ShopId = shop.ShopId,
            ProductName = dto.ProductName,
            ProductDescription = dto.ProductDescription,
            Price = dto.Price,
            StockQuantity = dto.StockQuantity,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };
        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);
        await ReplaceProductImageAsync(product, dto.ProductImageUrl, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToProductDto(product));
    }

    [HttpPut("inventory/{id:int}")]
    public async Task<ActionResult<ProductDto>> UpdateInventory(int id, UpsertProductDto dto, CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        var product = await db.Products.SingleOrDefaultAsync(x => x.ProductId == id && x.ShopId == shop.ShopId && x.IsActive, cancellationToken);
        if (product is null)
        {
            return NotFound(new { message = "Product was not found or has been deleted." });
        }

        product.ProductName = dto.ProductName;
        product.ProductDescription = dto.ProductDescription;
        product.Price = dto.Price;
        product.StockQuantity = dto.StockQuantity;
        product.IsActive = dto.IsActive;
        product.UpdatedAt = DateTime.UtcNow;
        await ReplaceProductImageAsync(product, dto.ProductImageUrl, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToProductDto(product));
    }

    [HttpDelete("inventory/{id:int}")]
    public async Task<IActionResult> DeleteInventory(int id, CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        var product = await db.Products.SingleOrDefaultAsync(x => x.ProductId == id && x.ShopId == shop.ShopId && x.IsActive, cancellationToken);
        if (product is null)
        {
            return NotFound(new { message = "Product was not found or has already been deleted." });
        }

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("payments")]
    public async Task<IActionResult> Payments(CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        return Ok(await db.Payments
            .Include(x => x.PaymentStatus)
            .Where(x => x.Request!.ShopId == shop.ShopId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.ToDto())
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("reviews")]
    public async Task<IActionResult> Reviews(CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        var mechanicIds = await db.ShopMechanics.Where(x => x.ShopId == shop.ShopId).Select(x => x.MechanicId).ToArrayAsync(cancellationToken);
        return Ok(await db.Reviews
            .Where(x => mechanicIds.Contains(x.MechanicId))
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ReviewDto(x.ReviewId, x.RequestId, x.MechanicId, x.Rating, x.Comment, x.CreatedAt))
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> Analytics(CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        var paidStatusId = await db.PaymentStatuses.Where(x => x.StatusName == "paid").Select(x => x.PaymentStatusId).SingleAsync(cancellationToken);
        return Ok(new
        {
            Bookings = await db.ServiceRequests.CountAsync(x => x.ShopId == shop.ShopId, cancellationToken),
            CompletedBookings = await db.ServiceRequests.CountAsync(x => x.ShopId == shop.ShopId && x.CurrentStatus!.StatusName == "completed", cancellationToken),
            Revenue = await db.Payments.Where(x => x.PaymentStatusId == paidStatusId && x.Request!.ShopId == shop.ShopId).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m,
            LowStockItems = await db.Products.CountAsync(x => x.ShopId == shop.ShopId && x.IsActive && x.StockQuantity <= 5, cancellationToken)
        });
    }

    private IQueryable<ShopServiceDto> QueryServices(int shopId, bool activeOnly = false, int? serviceId = null)
    {
        var query = db.ShopServices
            .Include(x => x.Category)
            .Where(x => x.ShopId == shopId);

        if (serviceId is int id)
        {
            query = query.Where(x => x.ShopServiceId == id);
        }

        if (activeOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        return query
            .Select(x => new ShopServiceDto(x.ShopServiceId, x.ShopId, x.CategoryId, x.Category!.CategoryName, x.ServiceName, x.ServiceDescription, x.BasePrice, x.EstimatedMinutes, x.IsActive));
    }

    private IQueryable<MechanicApplicationDto> QueryMechanicApplications(int shopId, int? mechanicId = null)
    {
        var query = db.ShopMechanics
            .Include(x => x.Shop)
            .Include(x => x.Mechanic).ThenInclude(x => x!.User)
            .Where(x => x.ShopId == shopId);

        if (mechanicId is int id)
        {
            query = query.Where(x => x.MechanicId == id);
        }

        return query
            .OrderByDescending(x => x.Mechanic!.CreatedAt)
            .Select(x => new MechanicApplicationDto(
                x.MechanicId,
                x.Mechanic!.UserId,
                x.Mechanic.User!.FirstName,
                x.Mechanic.MiddleName,
                x.Mechanic.User.LastName,
                x.Mechanic.User.FirstName + " " + x.Mechanic.User.LastName,
                x.Mechanic.User.Email,
                x.Mechanic.User.PhoneNumber,
                x.Mechanic.User.AccountStatus,
                x.Mechanic.User.EmailVerified,
                x.Mechanic.IsVerified,
                x.Mechanic.AvailabilityStatus,
                x.Mechanic.Sex,
                x.Mechanic.Birthdate,
                x.Mechanic.AddressLine,
                x.Mechanic.Barangay,
                x.Mechanic.City,
                x.Mechanic.Province,
                x.Mechanic.ZipCode,
                x.Mechanic.User.ProfileImageUrl,
                x.Mechanic.ValidIdImageUrl,
                x.Mechanic.CertificationImageUrl,
                x.Mechanic.Bio,
                x.Mechanic.YearsExperience,
                x.ShopId,
                x.Shop!.ShopName,
                x.IsActive,
                x.Mechanic.CreatedAt,
                x.Mechanic.UpdatedAt));
    }

    private IQueryable<ServiceRequestDto> QueryBookings(int shopId)
    {
        return db.ServiceRequests
            .Include(x => x.CurrentStatus)
            .Include(x => x.Client).ThenInclude(x => x!.User)
            .Include(x => x.Mechanic).ThenInclude(x => x!.User)
            .Include(x => x.Shop)
            .Include(x => x.ShopService)
            .Where(x => x.ShopId == shopId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(ServiceRequestService.ToDtoExpression());
    }

    private async Task<Shop> GetOwnedShopAsync(CancellationToken cancellationToken)
    {
        if (User.IsInRole(AppRoles.SystemAdmin))
        {
            return await db.Shops.OrderBy(x => x.ShopId).FirstAsync(cancellationToken);
        }

        var userId = User.GetUserId();
        return await db.Shops.OrderBy(x => x.ShopId).FirstAsync(x => x.OwnerUserId == userId, cancellationToken);
    }

    private async Task<Shop> GetOwnedShopWithOwnerAsync(CancellationToken cancellationToken)
    {
        var shops = db.Shops.Include(x => x.Owner).AsQueryable();
        if (User.IsInRole(AppRoles.SystemAdmin))
        {
            return await shops.OrderBy(x => x.ShopId).FirstAsync(cancellationToken);
        }

        var userId = User.GetUserId();
        return await shops.OrderBy(x => x.ShopId).FirstAsync(x => x.OwnerUserId == userId, cancellationToken);
    }

    private static ShopDetailsDto ToDetails(Shop shop)
    {
        return new ShopDetailsDto(shop.ShopId, shop.ShopName, RemoveDtiLine(shop.ShopDescription), shop.AddressLine, shop.City, shop.Province, shop.ContactNumber, shop.ShopStatus, shop.Latitude, shop.Longitude, shop.ShopImageUrl, shop.ShopLogoUrl);
    }

    private static ShopApplicationDetailsDto ToApplicationDetails(Shop shop)
    {
        var shopAddress = SplitStoredShopAddress(shop.AddressLine, shop.City, shop.Province);

        return new ShopApplicationDetailsDto(
            shop.ShopId,
            shop.ShopName,
            shop.ShopStatus,
            RemoveDtiLine(shop.ShopDescription),
            shopAddress.AddressLine,
            shopAddress.Barangay,
            shopAddress.City ?? shop.City,
            shopAddress.Province ?? shop.Province,
            shopAddress.ZipCode,
            shop.ContactNumber,
            shop.BusinessPermitUrl,
            shop.ShopImageUrl,
            shop.OwnerValidIdUrl,
            ExtractDtiRegistration(shop.ShopDescription),
            shop.Owner?.FirstName,
            shop.OwnerMiddleName,
            shop.Owner?.LastName,
            shop.Owner?.Email,
            shop.Owner?.PhoneNumber,
            shop.OwnerSex,
            shop.OwnerBirthdate,
            shop.OwnerAddressLine,
            shop.OwnerBarangay,
            shop.OwnerCity,
            shop.OwnerProvince,
            shop.OwnerZipCode,
            shop.Owner?.EmailVerified == true,
            shop.CreatedAt,
            shop.UpdatedAt);
    }

    private static StoredShopAddress SplitStoredShopAddress(string? addressLine, string? city, string? province)
    {
        if (string.IsNullOrWhiteSpace(addressLine))
        {
            return new StoredShopAddress(null, null, city, province, null);
        }

        var parts = addressLine
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToArray();

        if (parts.Length >= 5)
        {
            return new StoredShopAddress(
                string.Join(", ", parts.Take(parts.Length - 4)),
                parts[^4],
                parts[^3],
                parts[^2],
                parts[^1]);
        }

        return new StoredShopAddress(addressLine, null, city, province, null);
    }

    private static string? ExtractDtiRegistration(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var line = description
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(x => x.StartsWith("DTI Registration:", StringComparison.OrdinalIgnoreCase));

        return line?.Replace("DTI Registration:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
    }

    private static string? RemoveDtiLine(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var lines = description
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !x.StartsWith("DTI Registration:", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return lines.Length == 0 ? null : string.Join(Environment.NewLine, lines);
    }

    private static string? BuildShopDescription(string? description, string? dtiRegistration)
    {
        var cleanDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        var cleanDti = string.IsNullOrWhiteSpace(dtiRegistration) ? null : dtiRegistration.Trim();

        if (string.IsNullOrWhiteSpace(cleanDti))
        {
            return cleanDescription;
        }

        var dtiLine = $"DTI Registration: {cleanDti}";
        return string.IsNullOrWhiteSpace(cleanDescription)
            ? dtiLine
            : $"{cleanDescription}{Environment.NewLine}{dtiLine}";
    }

    private static string Require(string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{label} is required.");
        }

        return value.Trim();
    }

    private static string? CleanOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static System.Linq.Expressions.Expression<Func<Product, ProductDto>> ToProductDtoExpression()
    {
        return x => new ProductDto(
            x.ProductId,
            x.ShopId,
            x.ProductName,
            x.ProductDescription,
            x.Price,
            x.StockQuantity,
            x.IsActive,
            x.Images
                .OrderByDescending(image => image.CreatedAt)
                .Select(image => image.ImageUrl)
                .FirstOrDefault());
    }

    private static ProductDto ToProductDto(Product product)
    {
        return new ProductDto(
            product.ProductId,
            product.ShopId,
            product.ProductName,
            product.ProductDescription,
            product.Price,
            product.StockQuantity,
            product.IsActive,
            product.Images
                .OrderByDescending(image => image.CreatedAt)
                .Select(image => image.ImageUrl)
                .FirstOrDefault());
    }

    private async Task ReplaceProductImageAsync(Product product, string? imageUrl, CancellationToken cancellationToken)
    {
        imageUrl = CleanOptional(imageUrl);
        var existingImages = await db.ProductImages
            .Where(image => image.ProductId == product.ProductId)
            .ToArrayAsync(cancellationToken);

        if (existingImages.Length > 0)
        {
            db.ProductImages.RemoveRange(existingImages);
            product.Images.Clear();
        }

        if (imageUrl is null)
        {
            return;
        }

        product.Images.Add(new ProductImage
        {
            ProductId = product.ProductId,
            ImageUrl = imageUrl,
            CreatedAt = DateTime.UtcNow
        });
    }

    private sealed record StoredShopAddress(
        string? AddressLine,
        string? Barangay,
        string? City,
        string? Province,
        string? ZipCode);
}

