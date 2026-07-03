using BikeMate.Api.Helpers;
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
public sealed class ReviewsController(BikeMateDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ReviewDto>> Create(CreateReviewDto dto, CancellationToken cancellationToken)
    {
        if (dto.Rating is < 1 or > 5)
        {
            throw new InvalidOperationException("Rating must be between 1 and 5.");
        }

        var userId = User.GetUserId();
        var request = await db.ServiceRequests
            .Include(x => x.Client)
            .SingleAsync(x => x.RequestId == dto.RequestId && x.Client!.UserId == userId, cancellationToken);

        var mechanicId = request.MechanicId
            ?? await db.ShopMechanics
                .Where(x => x.ShopId == request.ShopId && x.IsActive)
                .Select(x => (int?)x.MechanicId)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No mechanic is assigned to this request yet.");

        var existing = await db.Reviews.SingleOrDefaultAsync(x => x.RequestId == request.RequestId, cancellationToken);
        if (existing is not null)
        {
            existing.Rating = dto.Rating;
            existing.Comment = dto.Comment;
            await db.SaveChangesAsync(cancellationToken);
            await RefreshMechanicReviewStatsAsync(existing.MechanicId, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return Ok(ToDto(existing));
        }

        var review = new Review
        {
            RequestId = request.RequestId,
            ClientId = request.ClientId,
            MechanicId = mechanicId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            CreatedAt = DateTime.UtcNow
        };

        db.Reviews.Add(review);
        await db.SaveChangesAsync(cancellationToken);
        await RefreshMechanicReviewStatsAsync(mechanicId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(review));
    }

    private async Task RefreshMechanicReviewStatsAsync(int mechanicId, CancellationToken cancellationToken)
    {
        var ratings = await db.Reviews
            .Where(x => x.MechanicId == mechanicId)
            .Select(x => x.Rating)
            .ToArrayAsync(cancellationToken);

        var mechanic = await db.Mechanics.SingleAsync(x => x.MechanicId == mechanicId, cancellationToken);
        mechanic.AverageRating = ratings.Length == 0
            ? 0m
            : Math.Round(ratings.Average(rating => (decimal)rating), 1);
        mechanic.UpdatedAt = DateTime.UtcNow;
    }

    private static ReviewDto ToDto(Review review)
    {
        return new ReviewDto(review.ReviewId, review.RequestId, review.MechanicId, review.Rating, review.Comment, review.CreatedAt);
    }
}
