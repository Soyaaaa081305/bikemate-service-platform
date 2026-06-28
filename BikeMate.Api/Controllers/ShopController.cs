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
public sealed class ShopController(BikeMateDbContext db) : ControllerBase
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
            InventoryAlerts = await db.Products.CountAsync(x => x.ShopId == shop.ShopId && x.StockQuantity <= 5, cancellationToken),
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

    [HttpPut("profile")]
    public async Task<ActionResult<ShopDetailsDto>> UpdateProfile(UpsertShopDto dto, CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        shop.ShopName = dto.ShopName;
        shop.ShopDescription = dto.ShopDescription;
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

    [HttpGet("services")]
    public async Task<ActionResult<IReadOnlyCollection<ShopServiceDto>>> Services(CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        return Ok(await QueryServices(shop.ShopId).ToArrayAsync(cancellationToken));
    }

    [HttpGet("services/{id:int}")]
    public async Task<ActionResult<ShopServiceDto>> Service(int id, CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        return Ok(await QueryServices(shop.ShopId).SingleAsync(x => x.ShopServiceId == id, cancellationToken));
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
        return Ok(await QueryServices(shop.ShopId).SingleAsync(x => x.ShopServiceId == service.ShopServiceId, cancellationToken));
    }

    [HttpPut("services/{id:int}")]
    public async Task<ActionResult<ShopServiceDto>> UpdateService(int id, UpsertShopServiceDto dto, CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        var service = await db.ShopServices.SingleAsync(x => x.ShopServiceId == id && x.ShopId == shop.ShopId, cancellationToken);
        service.CategoryId = dto.CategoryId;
        service.ServiceName = dto.ServiceName;
        service.ServiceDescription = dto.ServiceDescription;
        service.BasePrice = dto.BasePrice;
        service.EstimatedMinutes = dto.EstimatedMinutes;
        service.IsActive = dto.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(await QueryServices(shop.ShopId).SingleAsync(x => x.ShopServiceId == id, cancellationToken));
    }

    [HttpDelete("services/{id:int}")]
    public async Task<IActionResult> DeleteService(int id, CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        var service = await db.ShopServices.SingleAsync(x => x.ShopServiceId == id && x.ShopId == shop.ShopId, cancellationToken);
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
        return Ok(new { message = "Mechanic assigned." });
    }

    [HttpGet("mechanics")]
    public async Task<IActionResult> Mechanics(CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        return Ok(await db.ShopMechanics
            .Include(x => x.Mechanic).ThenInclude(x => x!.User)
            .Where(x => x.ShopId == shop.ShopId && x.IsActive)
            .Select(x => new MechanicProfileDto(x.MechanicId, x.Mechanic!.User!.FirstName + " " + x.Mechanic.User.LastName, x.Mechanic.Bio, x.Mechanic.YearsExperience, x.Mechanic.IsVerified, x.Mechanic.AvailabilityStatus, x.Mechanic.AverageRating, x.Mechanic.TotalCompletedJobs))
            .ToArrayAsync(cancellationToken));
    }

    [HttpPost("mechanics")]
    public async Task<IActionResult> AddMechanic(AssignMechanicDto dto, CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
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
        return Ok(await db.Products.Where(x => x.ShopId == shop.ShopId).Select(ToProductDtoExpression()).ToArrayAsync(cancellationToken));
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
        return Ok(ToProductDto(product));
    }

    [HttpPut("inventory/{id:int}")]
    public async Task<ActionResult<ProductDto>> UpdateInventory(int id, UpsertProductDto dto, CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        var product = await db.Products.SingleAsync(x => x.ProductId == id && x.ShopId == shop.ShopId, cancellationToken);
        product.ProductName = dto.ProductName;
        product.ProductDescription = dto.ProductDescription;
        product.Price = dto.Price;
        product.StockQuantity = dto.StockQuantity;
        product.IsActive = dto.IsActive;
        product.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToProductDto(product));
    }

    [HttpDelete("inventory/{id:int}")]
    public async Task<IActionResult> DeleteInventory(int id, CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(cancellationToken);
        var product = await db.Products.SingleAsync(x => x.ProductId == id && x.ShopId == shop.ShopId, cancellationToken);
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
            LowStockItems = await db.Products.CountAsync(x => x.ShopId == shop.ShopId && x.StockQuantity <= 5, cancellationToken)
        });
    }

    private IQueryable<ShopServiceDto> QueryServices(int shopId)
    {
        return db.ShopServices
            .Include(x => x.Category)
            .Where(x => x.ShopId == shopId)
            .Select(x => new ShopServiceDto(x.ShopServiceId, x.ShopId, x.CategoryId, x.Category!.CategoryName, x.ServiceName, x.ServiceDescription, x.BasePrice, x.EstimatedMinutes, x.IsActive));
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

    private static ShopDetailsDto ToDetails(Shop shop)
    {
        return new ShopDetailsDto(shop.ShopId, shop.ShopName, shop.ShopDescription, shop.AddressLine, shop.City, shop.Province, shop.ContactNumber, shop.ShopStatus, shop.Latitude, shop.Longitude);
    }

    private static System.Linq.Expressions.Expression<Func<Product, ProductDto>> ToProductDtoExpression()
    {
        return x => new ProductDto(x.ProductId, x.ShopId, x.ProductName, x.ProductDescription, x.Price, x.StockQuantity, x.IsActive);
    }

    private static ProductDto ToProductDto(Product product)
    {
        return new ProductDto(product.ProductId, product.ShopId, product.ProductName, product.ProductDescription, product.Price, product.StockQuantity, product.IsActive);
    }
}

