using BikeMate.Api.Helpers;
using BikeMate.Core.DTOs;
using BikeMate.Core.Entities;
using BikeMate.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BikeMate.Api.Controllers;

[ApiController]
[Route("api/devices")]
[Authorize]
public sealed class DevicesController(BikeMateDbContext db) : ControllerBase
{
    [HttpPost("register-token")]
    public async Task<IActionResult> RegisterToken(RegisterDeviceTokenDto dto, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var existing = await db.UserDeviceTokens.SingleOrDefaultAsync(x => x.UserId == userId && x.DeviceToken == dto.DeviceToken, cancellationToken);
        if (existing is null)
        {
            db.UserDeviceTokens.Add(new UserDeviceToken { UserId = userId, DeviceToken = dto.DeviceToken, Platform = dto.Platform, CreatedAt = DateTime.UtcNow });
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
