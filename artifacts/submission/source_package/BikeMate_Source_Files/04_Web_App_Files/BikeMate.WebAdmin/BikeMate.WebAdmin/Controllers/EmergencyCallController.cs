using System.Security.Claims;
using BikeMate.Core.DTOs;
using BikeMate.Infrastructure.Data;
using BikeMate.WebAdmin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BikeMate.WebAdmin.Controllers;

[Authorize(Roles = "SystemAdmin")]
[ApiController]
[Route("api/admin/emergency")]
public sealed class EmergencyCallController(
    BikeMateDbContext db,
    IWebAdminAgoraCallService agoraCallService) : ControllerBase
{
    [HttpPost("{requestId:int}/call/session")]
    public async Task<ActionResult<EmergencyCallSessionDto>> CreateSession(int requestId, CancellationToken cancellationToken)
    {
        var exists = await db.ServiceRequests.AnyAsync(x => x.RequestId == requestId, cancellationToken);
        if (!exists)
        {
            return NotFound(new { message = "Emergency request not found." });
        }

        var adminIdentity = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        return Ok(agoraCallService.CreateEmergencyCallSession(requestId, adminIdentity, DateTime.UtcNow));
    }
}
