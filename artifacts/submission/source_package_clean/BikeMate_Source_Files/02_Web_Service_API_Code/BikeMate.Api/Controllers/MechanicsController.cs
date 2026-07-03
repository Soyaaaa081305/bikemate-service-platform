using BikeMate.Api.Helpers;
using BikeMate.Api.Services;
using BikeMate.Core.Constants;
using BikeMate.Core.DTOs;
using BikeMate.Core.Entities;
using BikeMate.Core.Helpers;
using BikeMate.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BikeMate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.Mechanic)]
public sealed class MechanicsController(
    BikeMateDbContext db,
    IServiceRequestService serviceRequestService,
    ILocationService locationService) : ControllerBase
{
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<MechanicProfileDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var mechanic = await db.Mechanics.Include(x => x.User).SingleAsync(x => x.MechanicId == id, cancellationToken);
        return Ok(mechanic.ToProfileDto());
    }

    [HttpGet("me")]
    public async Task<ActionResult<MechanicProfileDto>> GetMe(CancellationToken cancellationToken)
    {
        var mechanic = await db.Mechanics.Include(x => x.User).SingleAsync(x => x.UserId == User.GetUserId(), cancellationToken);
        return Ok(mechanic.ToProfileDto());
    }

    [HttpGet("/api/mechanics/nearby")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyCollection<MechanicProfileDto>>> Nearby(
        [FromQuery] decimal? latitude,
        [FromQuery] decimal? longitude,
        CancellationToken cancellationToken)
    {
        var mechanics = await db.Mechanics
            .Include(x => x.User)
            .Where(x => x.IsVerified && x.AvailabilityStatus != "offline")
            .ToArrayAsync(cancellationToken);

        return Ok(mechanics
            .OrderBy(x => latitude is null || longitude is null || x.CurrentLatitude is null || x.CurrentLongitude is null
                ? 999m
                : GeoUtils.DistanceKm(latitude.Value, longitude.Value, x.CurrentLatitude.Value, x.CurrentLongitude.Value))
            .Take(20)
            .Select(x => x.ToProfileDto())
            .ToArray());
    }

    [HttpPut("me")]
    public async Task<ActionResult<MechanicProfileDto>> UpdateMe(UpdateMechanicProfileDto dto, CancellationToken cancellationToken)
    {
        var mechanic = await db.Mechanics.Include(x => x.User).SingleAsync(x => x.UserId == User.GetUserId(), cancellationToken);
        var hasPhoneUpdate = dto.PhoneNumber is not null;
        var phone = hasPhoneUpdate ? AuthService.NormalizePhilippineMobile(dto.PhoneNumber) : null;
        if (hasPhoneUpdate &&
            !string.IsNullOrWhiteSpace(phone) &&
            !string.Equals(mechanic.User!.PhoneNumber, phone, StringComparison.OrdinalIgnoreCase) &&
            await db.Users.AnyAsync(x => x.PhoneNumber == phone, cancellationToken))
        {
            throw new InvalidOperationException("Phone number is already registered.");
        }

        mechanic.Bio = dto.Bio ?? mechanic.Bio;
        mechanic.YearsExperience = dto.YearsExperience ?? mechanic.YearsExperience;
        mechanic.AvailabilityStatus = string.IsNullOrWhiteSpace(dto.AvailabilityStatus)
            ? mechanic.AvailabilityStatus
            : dto.AvailabilityStatus.Trim();
        mechanic.CurrentLatitude = dto.CurrentLatitude ?? mechanic.CurrentLatitude;
        mechanic.CurrentLongitude = dto.CurrentLongitude ?? mechanic.CurrentLongitude;
        if (hasPhoneUpdate)
        {
            mechanic.User!.PhoneNumber = phone;
            mechanic.User.UpdatedAt = DateTime.UtcNow;
        }

        mechanic.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(mechanic.ToProfileDto());
    }

    [HttpGet("jobs")]
    public async Task<ActionResult<IReadOnlyCollection<ServiceRequestDto>>> Jobs(CancellationToken cancellationToken)
    {
        var mechanicId = await GetMechanicIdAsync(cancellationToken);
        return Ok(await serviceRequestService.QueryForDto()
            .Where(x => x.MechanicId == mechanicId || x.MechanicId == null)
            .OrderByDescending(x => x.CreatedAt)
            .Select(ServiceRequestService.ToDtoExpression())
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("jobs/active")]
    public async Task<ActionResult<IReadOnlyCollection<ServiceRequestDto>>> ActiveJobs(CancellationToken cancellationToken)
    {
        var mechanicId = await GetMechanicIdAsync(cancellationToken);
        return Ok(await serviceRequestService.QueryForDto()
            .Where(x => x.MechanicId == mechanicId && x.CurrentStatus!.StatusName != "completed" && x.CurrentStatus.StatusName != "cancelled")
            .OrderByDescending(x => x.CreatedAt)
            .Select(ServiceRequestService.ToDtoExpression())
            .ToArrayAsync(cancellationToken));
    }

    [HttpPut("jobs/{requestId:int}/accept")]
    public async Task<ActionResult<ServiceRequestDto>> Accept(int requestId, CancellationToken cancellationToken)
    {
        var mechanicId = await GetMechanicIdAsync(cancellationToken);
        var request = await db.ServiceRequests.SingleAsync(x => x.RequestId == requestId, cancellationToken);
        request.MechanicId = mechanicId;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(await serviceRequestService.UpdateStatusAsync(requestId, "accepted", User.GetUserId(), "Accepted by mechanic.", cancellationToken));
    }

    [HttpPut("jobs/{requestId:int}/reject")]
    public async Task<ActionResult<ServiceRequestDto>> Reject(int requestId, CancellationToken cancellationToken)
    {
        return Ok(await serviceRequestService.UpdateStatusAsync(requestId, "rejected", User.GetUserId(), "Rejected by mechanic.", cancellationToken));
    }

    [HttpPut("jobs/{requestId:int}/status")]
    public async Task<ActionResult<ServiceRequestDto>> UpdateJobStatus(int requestId, UpdateRequestStatusDto dto, CancellationToken cancellationToken)
    {
        return Ok(await serviceRequestService.UpdateStatusAsync(requestId, dto.Status, User.GetUserId(), dto.Notes, cancellationToken));
    }

    [HttpPost("jobs/{requestId:int}/completion-photo")]
    public async Task<IActionResult> CompletionPhoto(int requestId, UploadMediaDto dto, CancellationToken cancellationToken)
    {
        db.RequestMedia.Add(new RequestMedia
        {
            RequestId = requestId,
            MediaUrl = dto.MediaUrl,
            MediaType = "completion_photo",
            Caption = dto.Caption,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Completion photo uploaded." });
    }

    [HttpPost("location")]
    public async Task<ActionResult<LiveLocationDto>> UpdateLocation(LocationUpdateDto dto, CancellationToken cancellationToken)
    {
        var mechanicId = await GetMechanicIdAsync(cancellationToken);
        return Ok(await locationService.UpdateAsync(dto with { MechanicId = mechanicId }, cancellationToken));
    }

    private Task<int> GetMechanicIdAsync(CancellationToken cancellationToken)
    {
        return db.GetMechanicIdAsync(User.GetUserId(), cancellationToken);
    }

}
