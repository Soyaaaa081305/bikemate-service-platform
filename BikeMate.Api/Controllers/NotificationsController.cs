using BikeMate.Api.Helpers;
using BikeMate.Core.DTOs;
using BikeMate.Core.Entities;
using BikeMate.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BikeMate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class NotificationsController(BikeMateDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<NotificationDto>>> GetMine(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        return Ok(await db.Notifications
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new NotificationDto(
                x.NotificationId,
                x.UserId,
                x.NotificationType,
                x.Title,
                x.Message,
                x.DataJson,
                x.IsRead,
                x.CreatedAt))
            .ToArrayAsync(cancellationToken));
    }

    [HttpPost("{id:int}/read")]
    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var notification = await db.Notifications.SingleAsync(x => x.NotificationId == id && x.UserId == userId, cancellationToken);
        notification.IsRead = true;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Notification marked read." });
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        await db.Notifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .ExecuteUpdateAsync(x => x.SetProperty(n => n.IsRead, true), cancellationToken);
        return Ok(new { message = "All notifications marked read." });
    }

    [HttpPost("register-device-token")]
    public async Task<IActionResult> RegisterDeviceToken(RegisterDeviceTokenDto dto, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var existing = await db.UserDeviceTokens
            .SingleOrDefaultAsync(x => x.UserId == userId && x.DeviceToken == dto.DeviceToken, cancellationToken);
        if (existing is null)
        {
            db.UserDeviceTokens.Add(new UserDeviceToken
            {
                UserId = userId,
                DeviceToken = dto.DeviceToken,
                Platform = dto.Platform,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.Platform = dto.Platform;
            existing.IsActive = true;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Device token registered." });
    }
}
