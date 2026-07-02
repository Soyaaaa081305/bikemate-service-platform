using BikeMate.Api.Helpers;
using BikeMate.Core.Constants;
using BikeMate.Core.DTOs;
using BikeMate.Core.Entities;
using BikeMate.Core.Services;
using BikeMate.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BikeMate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ShopsController(BikeMateDbContext db) : ControllerBase
{
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ShopDetailsDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await db.Shops
            .Where(x => x.ShopId == id)
            .Select(x => new ShopDetailsDto(x.ShopId, x.ShopName, x.ShopDescription, x.AddressLine, x.City, x.Province, x.ContactNumber, x.ShopStatus, x.Latitude, x.Longitude, x.ShopImageUrl, x.ShopLogoUrl))
            .SingleAsync(cancellationToken));
    }

    [HttpGet("{id:int}/reputation")]
    [AllowAnonymous]
    public async Task<ActionResult<ShopReputationDto>> Reputation(int id, CancellationToken cancellationToken)
    {
        if (!await db.Shops.AnyAsync(x => x.ShopId == id && x.ShopStatus == "verified", cancellationToken))
        {
            return NotFound(new { error = "Verified repair shop not found." });
        }

        var shopReviews = db.Reviews.Where(x => x.Request!.ShopId == id);
        var reviewCount = await shopReviews.CountAsync(cancellationToken);
        var averageRating = reviewCount == 0
            ? 0m
            : Math.Round(await shopReviews.AverageAsync(x => (decimal)x.Rating, cancellationToken), 1);
        var completedJobs = await db.ServiceRequests.CountAsync(
            x => x.ShopId == id &&
                 (x.CompletedAt != null || x.CurrentStatus!.StatusName == "completed"),
            cancellationToken);

        var topTechnicians = await db.ShopMechanics
            .Where(x => x.ShopId == id && x.IsActive && x.Mechanic!.IsVerified)
            .Select(x => new
            {
                x.MechanicId,
                FullName = x.Mechanic!.User!.FirstName + " " + x.Mechanic.User.LastName,
                x.Mechanic.User.ProfileImageUrl,
                Rating = db.Reviews
                    .Where(review => review.MechanicId == x.MechanicId && review.Request!.ShopId == id)
                    .Select(review => (decimal?)review.Rating)
                    .Average() ?? x.Mechanic.AverageRating,
                ReviewCount = db.Reviews.Count(review =>
                    review.MechanicId == x.MechanicId &&
                    review.Request!.ShopId == id),
                RecordedCompletedJobs = db.ServiceRequests.Count(request =>
                    request.ShopId == id &&
                    request.MechanicId == x.MechanicId &&
                    (request.CompletedAt != null || request.CurrentStatus!.StatusName == "completed")),
                ProfileCompletedJobs = x.Mechanic.TotalCompletedJobs,
                x.Mechanic.YearsExperience,
                x.Mechanic.AvailabilityStatus,
                x.Mechanic.IsVerified
            })
            .OrderByDescending(x => x.Rating)
            .ThenByDescending(x => x.ReviewCount)
            .ThenByDescending(x => x.RecordedCompletedJobs)
            .ThenByDescending(x => x.ProfileCompletedJobs)
            .Take(5)
            .ToArrayAsync(cancellationToken);

        var recentReviews = await shopReviews
            .OrderByDescending(x => x.CreatedAt)
            .Take(10)
            .Select(x => new ShopCustomerReviewDto(
                x.ReviewId,
                x.Rating,
                x.Comment,
                x.Client!.User!.FirstName + " " + x.Client.User.LastName,
                x.Mechanic!.User!.FirstName + " " + x.Mechanic.User.LastName,
                x.Request!.ShopService != null ? x.Request.ShopService.ServiceName : null,
                x.CreatedAt))
            .ToArrayAsync(cancellationToken);

        return Ok(new ShopReputationDto(
            id,
            averageRating,
            reviewCount,
            completedJobs,
            topTechnicians
                .Select(x => new ShopTechnicianSummaryDto(
                    x.MechanicId,
                    x.FullName,
                    x.ProfileImageUrl,
                    Math.Round(x.Rating, 1),
                    x.ReviewCount,
                    Math.Max(x.RecordedCompletedJobs, x.ProfileCompletedJobs),
                    x.YearsExperience,
                    x.AvailabilityStatus,
                    x.IsVerified))
                .ToArray(),
            recentReviews));
    }

    [HttpGet("my")]
    [Authorize(Roles = $"{AppRoles.ShopAdmin},{AppRoles.SystemAdmin}")]
    public async Task<ActionResult<IReadOnlyCollection<ShopDetailsDto>>> GetMine(CancellationToken cancellationToken)
    {
        if (User.IsInRole(AppRoles.SystemAdmin))
        {
            return Ok(await db.Shops
                .OrderBy(x => x.ShopId)
                .Select(x => new ShopDetailsDto(x.ShopId, x.ShopName, x.ShopDescription, x.AddressLine, x.City, x.Province, x.ContactNumber, x.ShopStatus, x.Latitude, x.Longitude, x.ShopImageUrl, x.ShopLogoUrl))
                .ToArrayAsync(cancellationToken));
        }

        var userId = User.GetUserId();
        return Ok(await db.Shops
            .Where(x => x.OwnerUserId == userId)
            .Select(x => new ShopDetailsDto(x.ShopId, x.ShopName, x.ShopDescription, x.AddressLine, x.City, x.Province, x.ContactNumber, x.ShopStatus, x.Latitude, x.Longitude, x.ShopImageUrl, x.ShopLogoUrl))
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("/api/shops/nearby")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyCollection<ShopSummaryDto>>> Nearby(
        [FromQuery] decimal? latitude,
        [FromQuery] decimal? longitude,
        CancellationToken cancellationToken,
        [FromQuery] decimal radiusKm = 50m,
        [FromQuery] string? concern = null)
    {
        var shops = await db.Shops
            .Include(x => x.Services)
            .ThenInclude(x => x.Category)
            .Where(x => x.ShopStatus == "verified")
            .ToArrayAsync(cancellationToken);

        var ranked = shops
            .Where(shop => shop.Services.Any(service =>
                service.IsActive &&
                RepairConcernMatcher.Matches(
                    concern,
                    service.Category?.CategoryName,
                    service.ServiceName,
                    service.ServiceDescription)))
            .Select(shop => new
            {
                Shop = shop,
                DistanceKm = latitude is null || longitude is null || shop.Latitude is null || shop.Longitude is null
                    ? (decimal?)null
                    : DistanceKm(latitude.Value, longitude.Value, shop.Latitude.Value, shop.Longitude.Value)
            })
            .Where(item =>
                latitude is null ||
                longitude is null ||
                (item.DistanceKm is not null && item.DistanceKm <= Math.Clamp(radiusKm, 1m, 200m)))
            .OrderBy(item => item.DistanceKm ?? decimal.MaxValue)
            .Take(20)
            .Select(item => item.Shop)
            .ToArray();

        return Ok(ranked
            .Select(shop => new ShopSummaryDto(
                shop.ShopId,
                shop.ShopName,
                shop.AddressLine,
                shop.City,
                shop.ContactNumber,
                shop.ShopStatus,
                shop.Latitude,
                shop.Longitude,
                shop.ShopImageUrl,
                shop.ShopLogoUrl,
                shop.Services.Count(service => service.IsActive),
                shop.Services.Where(service => service.IsActive).Select(service => (decimal?)service.BasePrice).Min()))
            .ToArray());
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.ShopAdmin)]
    public async Task<ActionResult<ShopDetailsDto>> Create(UpsertShopDto dto, CancellationToken cancellationToken)
    {
        var shop = new Shop
        {
            OwnerUserId = User.GetUserId(),
            ShopName = dto.ShopName,
            ShopDescription = dto.ShopDescription,
            AddressLine = dto.AddressLine,
            City = dto.City,
            Province = dto.Province,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            ContactNumber = dto.ContactNumber,
            ShopStatus = "pending",
            CreatedAt = DateTime.UtcNow
        };
        db.Shops.Add(shop);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToDetails(shop));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.ShopAdmin)]
    public async Task<ActionResult<ShopDetailsDto>> Update(int id, UpsertShopDto dto, CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(id, cancellationToken);
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

    [HttpPost("{id:int}/image")]
    [Authorize(Roles = AppRoles.ShopAdmin)]
    public async Task<IActionResult> UploadImage(int id, UploadMediaDto dto, CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(id, cancellationToken);
        shop.ShopImageUrl = dto.MediaUrl;
        shop.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { shop.ShopImageUrl });
    }

    [HttpPost("{id:int}/business-permit")]
    [Authorize(Roles = AppRoles.ShopAdmin)]
    public async Task<IActionResult> UploadPermit(int id, UploadMediaDto dto, CancellationToken cancellationToken)
    {
        var shop = await GetOwnedShopAsync(id, cancellationToken);
        shop.BusinessPermitUrl = dto.MediaUrl;
        shop.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { shop.BusinessPermitUrl });
    }

    [HttpGet("{id:int}/services")]
    [Authorize(Roles = AppRoles.ShopAdmin)]
    public async Task<ActionResult<IReadOnlyCollection<ShopServiceDto>>> GetServices(int id, CancellationToken cancellationToken)
    {
        await GetOwnedShopAsync(id, cancellationToken);
        return Ok(await db.ShopServices.Include(x => x.Category)
            .Where(x => x.ShopId == id)
            .Select(x => new ShopServiceDto(x.ShopServiceId, x.ShopId, x.CategoryId, x.Category!.CategoryName, x.ServiceName, x.ServiceDescription, x.BasePrice, x.EstimatedMinutes, x.IsActive))
            .ToArrayAsync(cancellationToken));
    }

    [HttpPost("{id:int}/services")]
    [Authorize(Roles = AppRoles.ShopAdmin)]
    public async Task<ActionResult<ShopServiceDto>> AddService(int id, UpsertShopServiceDto dto, CancellationToken cancellationToken)
    {
        await GetOwnedShopAsync(id, cancellationToken);
        var service = new ShopService
        {
            ShopId = id,
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
        var categoryName = await db.ServiceCategories.Where(x => x.CategoryId == dto.CategoryId).Select(x => x.CategoryName).SingleAsync(cancellationToken);
        return Ok(new ShopServiceDto(service.ShopServiceId, service.ShopId, service.CategoryId, categoryName, service.ServiceName, service.ServiceDescription, service.BasePrice, service.EstimatedMinutes, service.IsActive));
    }

    [HttpPut("{id:int}/services/{serviceId:int}")]
    [Authorize(Roles = AppRoles.ShopAdmin)]
    public async Task<ActionResult<ShopServiceDto>> UpdateService(int id, int serviceId, UpsertShopServiceDto dto, CancellationToken cancellationToken)
    {
        await GetOwnedShopAsync(id, cancellationToken);
        var service = await db.ShopServices.SingleAsync(x => x.ShopServiceId == serviceId && x.ShopId == id, cancellationToken);
        service.CategoryId = dto.CategoryId;
        service.ServiceName = dto.ServiceName;
        service.ServiceDescription = dto.ServiceDescription;
        service.BasePrice = dto.BasePrice;
        service.EstimatedMinutes = dto.EstimatedMinutes;
        service.IsActive = dto.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        var categoryName = await db.ServiceCategories.Where(x => x.CategoryId == dto.CategoryId).Select(x => x.CategoryName).SingleAsync(cancellationToken);
        return Ok(new ShopServiceDto(service.ShopServiceId, service.ShopId, service.CategoryId, categoryName, service.ServiceName, service.ServiceDescription, service.BasePrice, service.EstimatedMinutes, service.IsActive));
    }

    [HttpDelete("{id:int}/services/{serviceId:int}")]
    [Authorize(Roles = AppRoles.ShopAdmin)]
    public async Task<IActionResult> DeleteService(int id, int serviceId, CancellationToken cancellationToken)
    {
        await GetOwnedShopAsync(id, cancellationToken);
        var service = await db.ShopServices.SingleAsync(x => x.ShopServiceId == serviceId && x.ShopId == id, cancellationToken);
        db.ShopServices.Remove(service);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:int}/products")]
    [Authorize(Roles = AppRoles.ShopAdmin)]
    public async Task<ActionResult<IReadOnlyCollection<ProductDto>>> GetProducts(int id, CancellationToken cancellationToken)
    {
        await GetOwnedShopAsync(id, cancellationToken);
        return Ok(await db.Products
            .Include(x => x.Images)
            .Where(x => x.ShopId == id)
            .Select(x => new ProductDto(
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
                    .FirstOrDefault()))
            .ToArrayAsync(cancellationToken));
    }

    [HttpPost("{id:int}/products")]
    [Authorize(Roles = AppRoles.ShopAdmin)]
    public async Task<ActionResult<ProductDto>> AddProduct(int id, UpsertProductDto dto, CancellationToken cancellationToken)
    {
        await GetOwnedShopAsync(id, cancellationToken);
        var product = new Product
        {
            ShopId = id,
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

    [HttpPut("{id:int}/products/{productId:int}")]
    [Authorize(Roles = AppRoles.ShopAdmin)]
    public async Task<ActionResult<ProductDto>> UpdateProduct(int id, int productId, UpsertProductDto dto, CancellationToken cancellationToken)
    {
        await GetOwnedShopAsync(id, cancellationToken);
        var product = await db.Products.SingleAsync(x => x.ProductId == productId && x.ShopId == id, cancellationToken);
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

    [HttpDelete("{id:int}/products/{productId:int}")]
    [Authorize(Roles = AppRoles.ShopAdmin)]
    public async Task<IActionResult> DeleteProduct(int id, int productId, CancellationToken cancellationToken)
    {
        await GetOwnedShopAsync(id, cancellationToken);
        var product = await db.Products.SingleAsync(x => x.ProductId == productId && x.ShopId == id, cancellationToken);
        db.Products.Remove(product);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:int}/bookings")]
    [Authorize(Roles = AppRoles.ShopAdmin)]
    public async Task<ActionResult<IReadOnlyCollection<ServiceRequestDto>>> GetBookings(int id, CancellationToken cancellationToken)
    {
        await GetOwnedShopAsync(id, cancellationToken);
        return Ok(await db.ServiceRequests
            .Include(x => x.CurrentStatus)
            .Include(x => x.Client).ThenInclude(x => x!.User)
            .Include(x => x.Mechanic).ThenInclude(x => x!.User)
            .Include(x => x.Shop)
            .Include(x => x.ShopService)
            .Where(x => x.ShopId == id)
            .Select(BikeMate.Api.Services.ServiceRequestService.ToDtoExpression())
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("{id:int}/earnings")]
    [Authorize(Roles = AppRoles.ShopAdmin)]
    public async Task<IActionResult> GetEarnings(int id, CancellationToken cancellationToken)
    {
        await GetOwnedShopAsync(id, cancellationToken);
        var paidStatusId = await db.PaymentStatuses.Where(x => x.StatusName == "paid").Select(x => x.PaymentStatusId).SingleAsync(cancellationToken);
        var revenue = await db.Payments
            .Where(x => x.PaymentStatusId == paidStatusId && x.Request!.ShopId == id)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
        return Ok(new { shopId = id, revenue });
    }

    private async Task<Shop> GetOwnedShopAsync(int shopId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        return await db.Shops.SingleAsync(x => x.ShopId == shopId && x.OwnerUserId == userId, cancellationToken);
    }

    private static ShopDetailsDto ToDetails(Shop x)
    {
        return new ShopDetailsDto(x.ShopId, x.ShopName, x.ShopDescription, x.AddressLine, x.City, x.Province, x.ContactNumber, x.ShopStatus, x.Latitude, x.Longitude, x.ShopImageUrl, x.ShopLogoUrl);
    }

    private static ProductDto ToProductDto(Product x)
    {
        return new ProductDto(
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

    private async Task ReplaceProductImageAsync(Product product, string? imageUrl, CancellationToken cancellationToken)
    {
        imageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
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

    private static decimal DistanceKm(decimal latitudeA, decimal longitudeA, decimal latitudeB, decimal longitudeB)
    {
        const double earthRadiusKm = 6371d;
        var lat1 = ToRadians((double)latitudeA);
        var lat2 = ToRadians((double)latitudeB);
        var deltaLat = ToRadians((double)(latitudeB - latitudeA));
        var deltaLng = ToRadians((double)(longitudeB - longitudeA));
        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) *
                Math.Sin(deltaLng / 2) * Math.Sin(deltaLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Math.Round((decimal)(earthRadiusKm * c), 2);
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180d;
    }
}


