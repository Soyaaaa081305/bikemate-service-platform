using BikeMate.Api.Hubs;
using BikeMate.Api.Services;
using BikeMate.Core.DTOs;
using BikeMate.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BikeMate.Api.Controllers;

[ApiController]
[Route("api/location")]
[Authorize]
public sealed class LocationsController(
    BikeMateDbContext db,
    ILocationService locationService,
    IHubContext<LocationHub> locationHub) : ControllerBase
{
    [HttpPost("update")]
    public async Task<ActionResult<LiveLocationDto>> Update(LocationUpdateDto dto, CancellationToken cancellationToken)
    {
        var location = await locationService.UpdateAsync(dto, cancellationToken);
        if (location.RequestId is not null)
        {
            await locationHub.Clients.Group(BookingHub.GetRequestGroup(location.RequestId.Value))
                .SendAsync("MechanicLocationUpdated", location, cancellationToken);
        }

        return Ok(location);
    }

    [HttpGet("request/{requestId:int}/latest")]
    public async Task<ActionResult<LiveLocationDto>> LatestForRequest(int requestId, CancellationToken cancellationToken)
    {
        var location = await db.LiveLocations
            .Where(x => x.RequestId == requestId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new LiveLocationDto(x.LiveLocationId, x.RequestId, x.MechanicId, x.Latitude, x.Longitude, x.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
        if (location is not null)
        {
            return Ok(location);
        }

        var assignedMechanic = await db.ServiceRequests
            .Where(x => x.RequestId == requestId &&
                        x.MechanicId != null &&
                        x.Mechanic!.CurrentLatitude != null &&
                        x.Mechanic.CurrentLongitude != null)
            .Select(x => new LiveLocationDto(
                0,
                x.RequestId,
                x.MechanicId,
                x.Mechanic!.CurrentLatitude!.Value,
                x.Mechanic.CurrentLongitude!.Value,
                DateTime.UtcNow))
            .FirstOrDefaultAsync(cancellationToken);

        return assignedMechanic is null ? NotFound() : Ok(assignedMechanic);
    }

    [HttpGet("mechanic/{mechanicId:int}/latest")]
    public async Task<ActionResult<LiveLocationDto>> LatestForMechanic(int mechanicId, CancellationToken cancellationToken)
    {
        var location = await db.LiveLocations
            .Where(x => x.MechanicId == mechanicId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new LiveLocationDto(x.LiveLocationId, x.RequestId, x.MechanicId, x.Latitude, x.Longitude, x.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
        return location is null ? NotFound() : Ok(location);
    }
}
