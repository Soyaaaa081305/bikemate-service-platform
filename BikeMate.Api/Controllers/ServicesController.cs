using BikeMate.Core.DTOs;
using BikeMate.Core.Services;
using BikeMate.Core.Constants;
using BikeMate.Core.Entities;
using BikeMate.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BikeMate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ServicesController(BikeMateDbContext db) : ControllerBase
{
    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyCollection<ServiceCategoryDto>>> GetCategories(CancellationToken cancellationToken)
    {
        return Ok(await db.ServiceCategories
            .Where(x => x.IsActive)
            .OrderBy(x => x.CategoryName)
            .Select(x => new ServiceCategoryDto(x.CategoryId, x.CategoryName, x.Description))
            .ToArrayAsync(cancellationToken));
    }

    [HttpPost("categories")]
    [Authorize(Roles = $"{AppRoles.ShopAdmin},{AppRoles.SystemAdmin}")]
    public async Task<ActionResult<ServiceCategoryDto>> AddCategory(UpsertServiceCategoryDto dto, CancellationToken cancellationToken)
    {
        var categoryName = CleanCategoryName(dto.CategoryName);
        var description = CleanOptional(dto.Description);

        var existing = await db.ServiceCategories
            .FirstOrDefaultAsync(
                x => x.CategoryName.ToLower() == categoryName.ToLower(),
                cancellationToken);
        if (existing is not null)
        {
            existing.IsActive = true;
            if (!string.IsNullOrWhiteSpace(description))
            {
                existing.Description = description;
            }

            await db.SaveChangesAsync(cancellationToken);
            return Ok(new ServiceCategoryDto(existing.CategoryId, existing.CategoryName, existing.Description));
        }

        var category = new ServiceCategory
        {
            CategoryName = categoryName,
            Description = description,
            IsActive = true
        };

        db.ServiceCategories.Add(category);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new ServiceCategoryDto(category.CategoryId, category.CategoryName, category.Description));
    }

    [HttpGet("shops")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyCollection<ShopSummaryDto>>> GetShops(
        CancellationToken cancellationToken,
        [FromQuery] string? concern = null)
    {
        var shops = await db.Shops
            .Include(x => x.Services)
            .ThenInclude(x => x.Category)
            .Where(x => x.ShopStatus == "verified")
            .OrderBy(x => x.ShopName)
            .ToArrayAsync(cancellationToken);

        return Ok(shops
            .Where(shop => shop.Services.Any(service =>
                service.IsActive &&
                RepairConcernMatcher.Matches(
                    concern,
                    service.Category?.CategoryName,
                    service.ServiceName,
                    service.ServiceDescription)))
            .Select(x => new ShopSummaryDto(
                x.ShopId,
                x.ShopName,
                x.AddressLine,
                x.City,
                x.ContactNumber,
                x.ShopStatus,
                x.Latitude,
                x.Longitude,
                x.ShopImageUrl,
                x.ShopLogoUrl,
                x.Services.Count(service => service.IsActive),
                x.Services.Where(service => service.IsActive).Select(service => (decimal?)service.BasePrice).Min()))
            .ToArray());
    }

    [HttpGet("shops/{shopId:int}/services")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyCollection<ShopServiceDto>>> GetShopServices(int shopId, CancellationToken cancellationToken)
    {
        return Ok(await db.ShopServices
            .Include(x => x.Category)
            .Include(x => x.Shop)
            .Where(x => x.ShopId == shopId && x.IsActive && x.Shop!.ShopStatus == "verified")
            .Select(x => new ShopServiceDto(x.ShopServiceId, x.ShopId, x.CategoryId, x.Category!.CategoryName, x.ServiceName, x.ServiceDescription, x.BasePrice, x.EstimatedMinutes, x.IsActive))
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyCollection<ShopServiceDto>>> Search([FromQuery] string? q, CancellationToken cancellationToken)
    {
        var query = db.ShopServices
            .Include(x => x.Category)
            .Include(x => x.Shop)
            .Where(x => x.IsActive && x.Shop!.ShopStatus == "verified");
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(x => x.ServiceName.Contains(q) || (x.ServiceDescription != null && x.ServiceDescription.Contains(q)));
        }

        return Ok(await query
            .OrderBy(x => x.ServiceName)
            .Select(x => new ShopServiceDto(x.ShopServiceId, x.ShopId, x.CategoryId, x.Category!.CategoryName, x.ServiceName, x.ServiceDescription, x.BasePrice, x.EstimatedMinutes, x.IsActive))
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("nearby")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyCollection<ShopServiceDto>>> Nearby(
        [FromQuery] decimal? latitude,
        [FromQuery] decimal? longitude,
        [FromQuery] int? categoryId,
        CancellationToken cancellationToken)
    {
        var services = await db.ShopServices
            .Include(x => x.Category)
            .Include(x => x.Shop)
            .Where(x => x.IsActive && x.Shop!.ShopStatus == "verified" && (categoryId == null || x.CategoryId == categoryId))
            .ToArrayAsync(cancellationToken);

        return Ok(services
            .OrderBy(x => latitude is null || longitude is null || x.Shop!.Latitude is null || x.Shop.Longitude is null
                ? 999m
                : DistanceKm(latitude.Value, longitude.Value, x.Shop.Latitude.Value, x.Shop.Longitude.Value))
            .Select(x => new ShopServiceDto(x.ShopServiceId, x.ShopId, x.CategoryId, x.Category!.CategoryName, x.ServiceName, x.ServiceDescription, x.BasePrice, x.EstimatedMinutes, x.IsActive))
            .ToArray());
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

    private static string CleanCategoryName(string? value)
    {
        var name = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Category name is required.");
        }

        if (name.Length > 150)
        {
            throw new InvalidOperationException("Category name must be 150 characters or fewer.");
        }

        return name;
    }

    private static string? CleanOptional(string? value)
    {
        var clean = value?.Trim();
        return string.IsNullOrWhiteSpace(clean) ? null : clean;
    }
}
