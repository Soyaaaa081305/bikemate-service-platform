using BikeMate.Core.DTOs;
using BikeMate.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BikeMate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProductsController(BikeMateDbContext db) : ControllerBase
{
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyCollection<ProductDto>>> Search([FromQuery] string? q, CancellationToken cancellationToken)
    {
        var query = db.Products
            .Include(x => x.Shop)
            .Include(x => x.Images)
            .Where(x => x.IsActive && x.Shop!.ShopStatus == "verified");

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(x =>
                x.ProductName.Contains(q) ||
                (x.ProductDescription != null && x.ProductDescription.Contains(q)));
        }

        return Ok(await query
            .OrderBy(x => x.ProductName)
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
            .Take(50)
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("shop/{shopId:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyCollection<ProductDto>>> GetShopProducts(int shopId, CancellationToken cancellationToken)
    {
        return Ok(await db.Products
            .Include(x => x.Shop)
            .Include(x => x.Images)
            .Where(x => x.ShopId == shopId && x.IsActive && x.Shop!.ShopStatus == "verified")
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
}
