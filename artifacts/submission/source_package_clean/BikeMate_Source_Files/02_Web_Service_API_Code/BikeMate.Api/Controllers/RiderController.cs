using BikeMate.Api.Helpers;
using BikeMate.Api.Hubs;
using BikeMate.Api.Services;
using BikeMate.Core.Constants;
using BikeMate.Core.DTOs;
using BikeMate.Core.Entities;
using BikeMate.Core.Helpers;
using BikeMate.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BikeMate.Api.Controllers;

[ApiController]
[Route("api/rider")]
[Authorize(Roles = AppRoles.Mechanic)]
public sealed class RiderController(
    BikeMateDbContext db,
    IServiceRequestService serviceRequestService,
    ILocationService locationService,
    IBookingConversationService bookingConversationService,
    IHubContext<BookingHub> bookingHub,
    IHubContext<LocationHub> locationHub) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var mechanic = await GetMechanicAsync(cancellationToken);
        var activeStatuses = new[] { RequestStatuses.Paid, RequestStatuses.Accepted, RequestStatuses.EnRoute, RequestStatuses.Arrived, RequestStatuses.InProgress };
        var availableStatuses = new[] { RequestStatuses.Pending, RequestStatuses.Paid, RequestStatuses.PaymentPending };
        var activeJob = await serviceRequestService.QueryForDto()
            .Where(x => x.MechanicId == mechanic.MechanicId && activeStatuses.Contains(x.CurrentStatus!.StatusName))
            .OrderByDescending(x => x.CreatedAt)
            .Select(ServiceRequestService.ToDtoExpression())
            .FirstOrDefaultAsync(cancellationToken);
        activeJob = WithMechanicDistance(activeJob, mechanic);
        var incomingCount = await db.ServiceRequests.CountAsync(x =>
            x.MechanicId == null &&
            availableStatuses.Contains(x.CurrentStatus!.StatusName) &&
            !x.IssueDescription.StartsWith("[EMERGENCY]"),
            cancellationToken);
        var emergencyCount = await db.ServiceRequests.CountAsync(x => x.MechanicId == null && x.IssueDescription.StartsWith("[EMERGENCY]"), cancellationToken);
        var paidStatusId = await db.PaymentStatuses.Where(x => x.StatusName == "paid").Select(x => x.PaymentStatusId).SingleAsync(cancellationToken);
        var earnings = await db.Payments
            .Where(x => x.PaymentStatusId == paidStatusId && x.Request!.MechanicId == mechanic.MechanicId)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        return Ok(new
        {
            Profile = mechanic.ToProfileDto(),
            mechanic.AvailabilityStatus,
            ActiveJob = activeJob,
            IncomingRequests = incomingCount,
            EmergencyRequests = emergencyCount,
            TotalEarnings = earnings,
            mechanic.AverageRating,
            mechanic.TotalCompletedJobs,
            UnreadNotifications = await db.Notifications.CountAsync(x => x.UserId == mechanic.UserId && !x.IsRead, cancellationToken)
        });
    }

    [HttpPut("status/online")]
    public Task<IActionResult> GoOnline(CancellationToken cancellationToken)
    {
        return SetAvailabilityAsync("online", cancellationToken);
    }

    [HttpPut("status/offline")]
    public Task<IActionResult> GoOffline(CancellationToken cancellationToken)
    {
        return SetAvailabilityAsync("offline", cancellationToken);
    }

    [HttpGet("requests/incoming")]
    public async Task<ActionResult<IReadOnlyCollection<ServiceRequestDto>>> Incoming([FromQuery] decimal radiusKm = 8m, CancellationToken cancellationToken = default)
    {
        var mechanic = await GetMechanicAsync(cancellationToken);
        var availableStatuses = new[] { RequestStatuses.Pending, RequestStatuses.Paid, RequestStatuses.PaymentPending };
        var requests = await serviceRequestService.QueryForDto()
            .Where(x => x.MechanicId == null &&
                        availableStatuses.Contains(x.CurrentStatus!.StatusName) &&
                        !x.IssueDescription.StartsWith("[EMERGENCY]"))
            .OrderByDescending(x => x.CreatedAt)
            .Select(ServiceRequestService.ToDtoExpression())
            .ToArrayAsync(cancellationToken);

        return Ok(ToNearbyRequestDtos(requests, mechanic, radiusKm));
    }

    [HttpGet("requests/emergency")]
    public async Task<ActionResult<IReadOnlyCollection<ServiceRequestDto>>> Emergency([FromQuery] decimal radiusKm = 12m, CancellationToken cancellationToken = default)
    {
        var mechanic = await GetMechanicAsync(cancellationToken);
        var requests = await serviceRequestService.QueryForDto()
            .Where(x => x.MechanicId == null && x.IssueDescription.StartsWith("[EMERGENCY]"))
            .OrderByDescending(x => x.CreatedAt)
            .Select(ServiceRequestService.ToDtoExpression())
            .ToArrayAsync(cancellationToken);

        return Ok(ToNearbyRequestDtos(requests, mechanic, radiusKm));
    }

    [HttpGet("jobs/current")]
    public async Task<ActionResult<ServiceRequestDto?>> CurrentJob(CancellationToken cancellationToken)
    {
        var mechanic = await GetMechanicAsync(cancellationToken);
        var job = await serviceRequestService.QueryForDto()
            .Where(x => x.MechanicId == mechanic.MechanicId &&
                        x.CurrentStatus!.StatusName != RequestStatuses.Completed &&
                        x.CurrentStatus.StatusName != RequestStatuses.Cancelled &&
                        x.CurrentStatus.StatusName != RequestStatuses.Rejected &&
                        x.CurrentStatus.StatusName != RequestStatuses.Pending &&
                        x.CurrentStatus.StatusName != RequestStatuses.PaymentPending)
            .OrderByDescending(x => x.CreatedAt)
            .Select(ServiceRequestService.ToDtoExpression())
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(WithMechanicDistance(job, mechanic));
    }

    [HttpGet("jobs/history")]
    public async Task<ActionResult<IReadOnlyCollection<ServiceRequestDto>>> History(CancellationToken cancellationToken)
    {
        var mechanic = await GetMechanicAsync(cancellationToken);
        var jobs = await serviceRequestService.QueryForDto()
            .Where(x => x.MechanicId == mechanic.MechanicId &&
                        (x.CurrentStatus!.StatusName == "completed" ||
                         x.CurrentStatus.StatusName == "cancelled" ||
                         x.CurrentStatus.StatusName == "rejected"))
            .OrderByDescending(x => x.CreatedAt)
            .Select(ServiceRequestService.ToDtoExpression())
            .ToArrayAsync(cancellationToken);

        return Ok(WithMechanicDistances(jobs, mechanic));
    }

    [HttpGet("jobs/{id:int}")]
    public async Task<ActionResult<ServiceRequestDto>> JobDetails(int id, CancellationToken cancellationToken)
    {
        var mechanic = await GetMechanicAsync(cancellationToken);
        var availableStatuses = new[] { RequestStatuses.Pending, RequestStatuses.Paid, RequestStatuses.PaymentPending };
        var job = await serviceRequestService.QueryForDto()
            .Where(x => x.RequestId == id &&
                        (x.MechanicId == mechanic.MechanicId ||
                         x.MechanicId == null && availableStatuses.Contains(x.CurrentStatus!.StatusName)))
            .Select(ServiceRequestService.ToDtoExpression())
            .SingleAsync(cancellationToken);

        return Ok(WithMechanicDistance(job, mechanic));
    }

    [HttpPut("jobs/{id:int}/accept")]
    public async Task<ActionResult<ServiceRequestDto>> Accept(int id, CancellationToken cancellationToken)
    {
        var mechanicId = await GetMechanicIdAsync(cancellationToken);
        var request = await db.ServiceRequests
            .Include(x => x.CurrentStatus)
            .SingleAsync(x => x.RequestId == id, cancellationToken);
        if (request.MechanicId is not null && request.MechanicId != mechanicId)
        {
            return Forbid();
        }

        if (request.CurrentStatus?.StatusName is not "pending" and not "paid" and not "payment_pending")
        {
            return BadRequest(new { error = "This job is no longer available to accept." });
        }

        request.MechanicId = mechanicId;
        await db.SaveChangesAsync(cancellationToken);
        var dto = await serviceRequestService.UpdateStatusAsync(id, "accepted", User.GetUserId(), "Accepted by rider.", cancellationToken);
        await bookingConversationService.SyncRequestAsync(id, cancellationToken);
        await bookingHub.Clients.Group(BookingHub.GetRequestGroup(id)).SendAsync("ServiceRequestAccepted", dto, cancellationToken);
        return Ok(dto);
    }

    [HttpPut("jobs/{id:int}/reject")]
    public async Task<ActionResult<ServiceRequestDto>> Reject(int id, CancellationToken cancellationToken)
    {
        var mechanicId = await GetMechanicIdAsync(cancellationToken);
        var request = await db.ServiceRequests.SingleAsync(x => x.RequestId == id, cancellationToken);
        if (request.MechanicId is not null && request.MechanicId != mechanicId)
        {
            return Forbid();
        }

        var dto = await serviceRequestService.UpdateStatusAsync(id, "rejected", User.GetUserId(), "Rejected by rider.", cancellationToken);
        await bookingHub.Clients.Group(BookingHub.GetRequestGroup(id)).SendAsync("ServiceStatusChanged", dto, cancellationToken);
        return Ok(dto);
    }

    [HttpPut("jobs/{id:int}/status")]
    public async Task<ActionResult<ServiceRequestDto>> UpdateStatus(int id, UpdateRequestStatusDto dto, CancellationToken cancellationToken)
    {
        var mechanicId = await GetMechanicIdAsync(cancellationToken);
        var ownsJob = await db.ServiceRequests.AnyAsync(x => x.RequestId == id && x.MechanicId == mechanicId, cancellationToken);
        if (!ownsJob)
        {
            return Forbid();
        }

        var mappedStatus = dto.Status switch
        {
            "on_the_way" => "en_route",
            "service_started" => "in_progress",
            "service_completed" => "completed",
            _ => dto.Status
        };
        var updated = await serviceRequestService.UpdateStatusAsync(id, mappedStatus, User.GetUserId(), dto.Notes, cancellationToken);
        await bookingHub.Clients.Group(BookingHub.GetRequestGroup(id)).SendAsync("ServiceStatusChanged", updated, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("jobs/{id:int}/before-photo")]
    public Task<IActionResult> BeforePhoto(int id, UploadMediaDto dto, CancellationToken cancellationToken)
    {
        return AddJobPhotoAsync(id, "before_photo", dto, cancellationToken);
    }

    [HttpPost("jobs/{id:int}/after-photo")]
    public Task<IActionResult> AfterPhoto(int id, UploadMediaDto dto, CancellationToken cancellationToken)
    {
        return AddJobPhotoAsync(id, "after_photo", dto, cancellationToken);
    }

    [HttpPost("location")]
    public async Task<ActionResult<LiveLocationDto>> UpdateLocation(LocationUpdateDto dto, CancellationToken cancellationToken)
    {
        var mechanicId = await GetMechanicIdAsync(cancellationToken);
        var location = await locationService.UpdateAsync(dto with { MechanicId = mechanicId }, cancellationToken);
        if (location.RequestId is not null)
        {
            await locationHub.Clients.Group(BookingHub.GetRequestGroup(location.RequestId.Value))
                .SendAsync("MechanicLocationUpdated", location, cancellationToken);
        }

        return Ok(location);
    }

    [HttpGet("earnings")]
    public async Task<IActionResult> Earnings(CancellationToken cancellationToken)
    {
        var mechanicId = await GetMechanicIdAsync(cancellationToken);
        var paidStatusId = await db.PaymentStatuses.Where(x => x.StatusName == "paid").Select(x => x.PaymentStatusId).SingleAsync(cancellationToken);
        var payments = await db.Payments
            .Include(x => x.Request)
            .Where(x => x.PaymentStatusId == paidStatusId && x.Request!.MechanicId == mechanicId)

            .OrderByDescending(x => x.PaidAt ?? x.CreatedAt)
            .Select(x => x.ToDto("paid"))
            .ToArrayAsync(cancellationToken);

        return Ok(new { Total = payments.Sum(x => x.Amount), Payments = payments });
    }

    [HttpGet("ratings")]
    public async Task<IActionResult> Ratings(CancellationToken cancellationToken)
    {
        var mechanicId = await GetMechanicIdAsync(cancellationToken);
        return Ok(await db.Reviews
            .Where(x => x.MechanicId == mechanicId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ReviewDto(x.ReviewId, x.RequestId, x.MechanicId, x.Rating, x.Comment, x.CreatedAt))
            .ToArrayAsync(cancellationToken));
    }

    private async Task<IActionResult> SetAvailabilityAsync(string status, CancellationToken cancellationToken)
    {
        var mechanic = await GetMechanicAsync(cancellationToken);
        mechanic.AvailabilityStatus = status;
        mechanic.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(mechanic.ToProfileDto());
    }

    private async Task<IActionResult> AddJobPhotoAsync(int requestId, string mediaType, UploadMediaDto dto, CancellationToken cancellationToken)
    {
        var mechanicId = await GetMechanicIdAsync(cancellationToken);
        var ownsJob = await db.ServiceRequests.AnyAsync(x => x.RequestId == requestId && x.MechanicId == mechanicId, cancellationToken);
        if (!ownsJob)
        {
            return Forbid();
        }

        var savedMediaType = string.IsNullOrWhiteSpace(dto.MediaType) ? mediaType : dto.MediaType.Trim();
        if (savedMediaType is not "before_photo" and not "after_photo" and not "completion_image" and not "completion_video")
        {
            savedMediaType = mediaType;
        }

        db.RequestMedia.Add(new RequestMedia
        {
            RequestId = requestId,
            MediaUrl = dto.MediaUrl,
            MediaType = savedMediaType,
            Caption = dto.Caption,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Job photo uploaded." });
    }

    private Task<int> GetMechanicIdAsync(CancellationToken cancellationToken)
    {
        return db.GetMechanicIdAsync(User.GetUserId(), cancellationToken);
    }

    private Task<Mechanic> GetMechanicAsync(CancellationToken cancellationToken)
    {
        return db.GetMechanicAsync(User.GetUserId(), cancellationToken);
    }

    private static IReadOnlyCollection<ServiceRequestDto> ToNearbyRequestDtos(IEnumerable<ServiceRequestDto> requests, Mechanic mechanic, decimal radiusKm)
    {
        var mechanicHasLocation = mechanic.CurrentLatitude is not null && mechanic.CurrentLongitude is not null;

        var rows = requests
            .Select(request => new
            {
                Request = request,
                Distance = DistanceFromMechanic(request, mechanic)
            })
            .Where(x => !mechanicHasLocation || x.Distance is not null && x.Distance <= radiusKm)
            .OrderBy(x => x.Distance ?? 999m)
            .ThenByDescending(x => x.Request.CreatedAt)
            .Take(30)
            .Select(x => x.Request with { DistanceKm = x.Distance })
            .ToArray();

        return rows;
    }

    private static IReadOnlyCollection<ServiceRequestDto> WithMechanicDistances(IEnumerable<ServiceRequestDto> requests, Mechanic mechanic)
    {
        return requests
            .Select(request => WithMechanicDistance(request, mechanic)!)
            .ToArray();
    }

    private static ServiceRequestDto? WithMechanicDistance(ServiceRequestDto? request, Mechanic mechanic)
    {
        return request is null
            ? null
            : request with { DistanceKm = DistanceFromMechanic(request, mechanic) };
    }

    private static decimal? DistanceFromMechanic(ServiceRequestDto request, Mechanic mechanic)
    {
        return mechanic.CurrentLatitude is not null &&
               mechanic.CurrentLongitude is not null &&
               request.ServiceLatitude is not null &&
               request.ServiceLongitude is not null
            ? GeoUtils.DistanceKm(mechanic.CurrentLatitude.Value, mechanic.CurrentLongitude.Value, request.ServiceLatitude.Value, request.ServiceLongitude.Value)
            : null;
    }

}
