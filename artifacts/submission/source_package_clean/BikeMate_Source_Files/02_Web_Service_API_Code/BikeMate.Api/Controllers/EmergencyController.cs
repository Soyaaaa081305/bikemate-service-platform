using BikeMate.Api.Helpers;
using BikeMate.Api.Hubs;
using BikeMate.Api.Services;
using BikeMate.Core.Constants;
using BikeMate.Core.DTOs;
using BikeMate.Core.Entities;
using BikeMate.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BikeMate.Api.Controllers;

[ApiController]
[Route("api/emergency")]
[Authorize]
public sealed class EmergencyController(
    BikeMateDbContext db,
    IBookingConversationService bookingConversationService,
    IHubContext<EmergencyHub> hubContext,
    IAgoraTokenService agoraTokenService) : ControllerBase
{
    [HttpPost("request")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<EmergencyRequestStatusDto>> Create(CreateEmergencyRequestDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.ServiceLocation))
        {
            return BadRequest(new { message = "Service location is required." });
        }

        var userId = User.GetUserId();
        var client = await db.Clients
            .Include(x => x.User)
            .Include(x => x.Motorcycles)
            .SingleAsync(x => x.UserId == userId, cancellationToken);
        if (!string.Equals(client.User?.AccountStatus, "active", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Your BikeMate account is still waiting for admin approval. Booking opens only after approval." });
        }

        var emergencyStatusId = await GetOrCreateStatusIdAsync("emergency_pending", cancellationToken);
        var emergencyService = await db.ShopServices
            .Include(x => x.Category)
            .Where(x => x.IsActive &&
                (EF.Functions.Like(x.ServiceName, "%Emergency%") ||
                 EF.Functions.Like(x.Category!.CategoryName, "%Emergency%")))
            .OrderBy(x => x.BasePrice)
            .FirstOrDefaultAsync(cancellationToken);

        var request = new ServiceRequest
        {
            ClientId = client.ClientId,
            ShopId = emergencyService?.ShopId,
            ShopServiceId = emergencyService?.ShopServiceId,
            MotorcycleId = client.Motorcycles.FirstOrDefault()?.MotorcycleId,
            CurrentStatusId = emergencyStatusId,
            IssueDescription = $"[EMERGENCY] {CleanNotes(dto.Notes)}",
            ServiceLocationAddress = dto.ServiceLocation.Trim(),
            ServiceLatitude = dto.Latitude,
            ServiceLongitude = dto.Longitude,
            EstimatedTotal = 0m,
            CreatedAt = DateTime.UtcNow
        };

        db.ServiceRequests.Add(request);
        await db.SaveChangesAsync(cancellationToken);

        db.RequestStatusHistory.Add(new RequestStatusHistory
        {
            RequestId = request.RequestId,
            NewStatusId = emergencyStatusId,
            ChangedByUserId = userId,
            Notes = $"Emergency request created. Level: {dto.EmergencyLevel ?? "Roadside"}.",
            CreatedAt = DateTime.UtcNow
        });

        await NotifyRespondersAsync(request, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await bookingConversationService.EnsureEmergencySupportConversationAsync(request.RequestId, cancellationToken);

        var status = await BuildStatusDtoAsync(request.RequestId, cancellationToken);
        await hubContext.Clients.Group(EmergencyHub.GetEmergencyGroup(request.RequestId))
            .SendAsync("EmergencyRequestCreated", status, cancellationToken);
        return Ok(status);
    }

    [HttpGet("request/{requestId:int}")]
    public async Task<ActionResult<EmergencyRequestStatusDto>> GetStatus(int requestId, CancellationToken cancellationToken)
    {
        await EnsureCanViewRequestAsync(requestId, cancellationToken);
        return Ok(await BuildStatusDtoAsync(requestId, cancellationToken));
    }

    [HttpGet("request/{requestId:int}/conversation")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<ConversationDto>> GetConversation(int requestId, CancellationToken cancellationToken)
    {
        await GetCustomerRequestAsync(requestId, cancellationToken);
        var conversationId = await bookingConversationService
            .EnsureEmergencySupportConversationAsync(requestId, cancellationToken);
        if (conversationId is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "BikeMate Emergency support is not configured yet."
            });
        }

        return Ok(new ConversationDto(conversationId.Value, requestId, "emergency_support", DateTime.UtcNow));
    }

    [HttpPut("request/{requestId:int}/cancel")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<EmergencyRequestStatusDto>> Cancel(int requestId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var request = await db.ServiceRequests
            .Include(x => x.Client)
            .SingleAsync(x => x.RequestId == requestId && x.Client!.UserId == userId, cancellationToken);

        await SetStatusAsync(request, "cancelled", userId, "Emergency request cancelled by customer.", cancellationToken);
        var status = await BuildStatusDtoAsync(requestId, cancellationToken);
        await hubContext.Clients.Group(EmergencyHub.GetEmergencyGroup(requestId))
            .SendAsync("EmergencyRequestCancelled", status, cancellationToken);
        return Ok(status);
    }

    [HttpPut("request/{requestId:int}/accept")]
    [Authorize(Roles = AppRoles.Mechanic)]
    public async Task<ActionResult<EmergencyRequestStatusDto>> Accept(int requestId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var mechanic = await db.Mechanics.Include(x => x.User).SingleAsync(x => x.UserId == userId, cancellationToken);
        var request = await db.ServiceRequests.Include(x => x.Client).SingleAsync(x => x.RequestId == requestId, cancellationToken);
        request.MechanicId = mechanic.MechanicId;
        request.AcceptedAt = DateTime.UtcNow;

        await SetStatusAsync(request, "accepted", userId, "Emergency request accepted by responder.", cancellationToken);
        await bookingConversationService.EnsureEmergencySupportConversationAsync(requestId, cancellationToken);
        db.Notifications.Add(new Notification
        {
            UserId = request.Client!.UserId,
            NotificationType = "emergency",
            Title = "Responder assigned",
            Message = $"{mechanic.User!.FirstName} accepted your emergency request.",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);

        var status = await BuildStatusDtoAsync(requestId, cancellationToken);
        await hubContext.Clients.Group(EmergencyHub.GetEmergencyGroup(requestId))
            .SendAsync("EmergencyResponderAssigned", status, cancellationToken);
        return Ok(status);
    }

    [HttpPost("request/{requestId:int}/call/start")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<EmergencyCallSessionDto>> StartCall(int requestId, CancellationToken cancellationToken)
    {
        var request = await GetCustomerRequestAsync(requestId, cancellationToken);
        var startedAt = DateTime.UtcNow;
        var session = agoraTokenService.CreateEmergencyCallSession(requestId, User.GetUserId(), startedAt);
        var status = string.Equals(session.CallStatus, "TokenReady", StringComparison.OrdinalIgnoreCase)
            ? "call_connected"
            : "call_connecting";
        await SetStatusAsync(request, status, User.GetUserId(), session.Message, cancellationToken);
        await hubContext.Clients.Group(EmergencyHub.GetEmergencyGroup(requestId))
            .SendAsync("EmergencyCallConnected", session, cancellationToken);

        return Ok(session);
    }

    [HttpPost("request/{requestId:int}/call/session")]
    public async Task<ActionResult<EmergencyCallSessionDto>> CreateCallSession(int requestId, CancellationToken cancellationToken)
    {
        await EnsureCanViewRequestAsync(requestId, cancellationToken);
        var session = agoraTokenService.CreateEmergencyCallSession(requestId, User.GetUserId(), DateTime.UtcNow);
        return Ok(session);
    }

    [HttpPost("request/{requestId:int}/call/end")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<EmergencyCallSessionDto>> EndCall(int requestId, CancellationToken cancellationToken)
    {
        var request = await GetCustomerRequestAsync(requestId, cancellationToken);
        var nextStatus = request.MechanicId is null ? "searching_responder" : "accepted";
        await SetStatusAsync(request, nextStatus, User.GetUserId(), "Emergency support call ended; request remains active.", cancellationToken);
        await hubContext.Clients.Group(EmergencyHub.GetEmergencyGroup(requestId))
            .SendAsync("EmergencyCallEnded", requestId, cancellationToken);

        return Ok(new EmergencyCallSessionDto(
            requestId,
            "Ended",
            DateTime.UtcNow,
            DateTime.UtcNow,
            "Emergency call ended. Your request remains active."));
    }

    [HttpGet("responders/nearby")]
    public async Task<ActionResult<IReadOnlyCollection<NearbyResponderDto>>> NearbyResponders(
        [FromQuery] decimal latitude,
        [FromQuery] decimal longitude,
        CancellationToken cancellationToken)
    {
        return Ok(await GetNearbyRespondersAsync(latitude, longitude, cancellationToken));
    }

    private async Task<ServiceRequest> GetCustomerRequestAsync(int requestId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        return await db.ServiceRequests
            .Include(x => x.Client)
            .SingleAsync(x => x.RequestId == requestId && x.Client!.UserId == userId, cancellationToken);
    }

    private async Task EnsureCanViewRequestAsync(int requestId, CancellationToken cancellationToken)
    {
        if (User.IsInRole(AppRoles.SystemAdmin) || User.IsInRole(AppRoles.ShopAdmin))
        {
            return;
        }

        var userId = User.GetUserId();
        var canView = await db.ServiceRequests.AnyAsync(x =>
            x.RequestId == requestId &&
            (x.Client!.UserId == userId || x.Mechanic!.UserId == userId), cancellationToken);

        if (!canView)
        {
            throw new UnauthorizedAccessException("You cannot view this emergency request.");
        }
    }

    private async Task<EmergencyRequestStatusDto> BuildStatusDtoAsync(int requestId, CancellationToken cancellationToken)
    {
        var request = await db.ServiceRequests
            .Include(x => x.CurrentStatus)
            .Include(x => x.Client).ThenInclude(x => x!.User)
            .Include(x => x.Mechanic).ThenInclude(x => x!.User)
            .Include(x => x.LiveLocations)
            .SingleAsync(x => x.RequestId == requestId, cancellationToken);

        var customerLatitude = request.ServiceLatitude ?? 0m;
        var customerLongitude = request.ServiceLongitude ?? 0m;
        var latestMechanicLocation = request.LiveLocations
            .Where(x => x.MechanicId == request.MechanicId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();
        var nearby = customerLatitude == 0m && customerLongitude == 0m
            ? []
            : await GetNearbyRespondersAsync(customerLatitude, customerLongitude, cancellationToken);

        var status = ToEmergencyStatus(request.CurrentStatus?.StatusName, request.MechanicId);
        return new EmergencyRequestStatusDto(
            request.RequestId,
            status,
            BuildStatusMessage(status, nearby.Count),
            request.MechanicId,
            request.Mechanic is null ? null : $"{request.Mechanic.User!.FirstName} {request.Mechanic.User.LastName}",
            request.Mechanic?.User?.PhoneNumber,
            customerLatitude,
            customerLongitude,
            request.ServiceLocationAddress ?? "Current location",
            latestMechanicLocation?.Latitude ?? request.Mechanic?.CurrentLatitude,
            latestMechanicLocation?.Longitude ?? request.Mechanic?.CurrentLongitude,
            nearby,
            request.CreatedAt);
    }

    private async Task<IReadOnlyCollection<NearbyResponderDto>> GetNearbyRespondersAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken)
    {
        var mechanics = await db.Mechanics
            .Include(x => x.User)
            .Where(x => x.CurrentLatitude != null &&
                        x.CurrentLongitude != null &&
                        x.AvailabilityStatus != "offline")
            .ToArrayAsync(cancellationToken);

        return mechanics
            .Select(x =>
            {
                var distanceKm = DistanceKm(latitude, longitude, x.CurrentLatitude!.Value, x.CurrentLongitude!.Value);
                return new NearbyResponderDto(
                    x.MechanicId,
                    $"{x.User!.FirstName} {x.User.LastName}",
                    x.User.ProfileImageUrl,
                    x.CurrentLatitude,
                    x.CurrentLongitude,
                    distanceKm,
                    x.AverageRating,
                    x.AvailabilityStatus);
            })
            .Where(x => x.DistanceKm is null or <= 30m)
            .OrderBy(x => x.DistanceKm ?? 999m)
            .Take(5)
            .ToArray();
    }

    private async Task NotifyRespondersAsync(ServiceRequest request, CancellationToken cancellationToken)
    {
        var responders = await GetNearbyResponderUsersAsync(request.ServiceLatitude ?? 0m, request.ServiceLongitude ?? 0m, cancellationToken);
        foreach (var responder in responders)
        {
            db.Notifications.Add(new Notification
            {
                UserId = responder.UserId,
                NotificationType = "emergency",
                Title = "Emergency request nearby",
                Message = $"A customer needs emergency roadside help near {request.ServiceLocationAddress}.",
                CreatedAt = DateTime.UtcNow,
                DataJson = $$"""{"requestId":{{request.RequestId}}}"""
            });
        }

        var adminUserIds = await db.UserRoles
            .Where(x => x.Role!.RoleName == AppRoles.SystemAdmin)
            .Select(x => x.UserId)
            .ToArrayAsync(cancellationToken);

        foreach (var adminUserId in adminUserIds)
        {
            db.Notifications.Add(new Notification
            {
                UserId = adminUserId,
                NotificationType = "emergency",
                Title = "Emergency request created",
                Message = $"Emergency request #{request.RequestId} needs monitoring.",
                CreatedAt = DateTime.UtcNow,
                DataJson = $$"""{"requestId":{{request.RequestId}}}"""
            });
        }
    }

    private async Task<IReadOnlyCollection<Mechanic>> GetNearbyResponderUsersAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken)
    {
        var mechanics = await db.Mechanics
            .Include(x => x.User)
            .Where(x => x.CurrentLatitude != null &&
                        x.CurrentLongitude != null &&
                        x.AvailabilityStatus != "offline")
            .ToArrayAsync(cancellationToken);

        return mechanics
            .Select(x => new { Mechanic = x, Distance = DistanceKm(latitude, longitude, x.CurrentLatitude!.Value, x.CurrentLongitude!.Value) })
            .Where(x => x.Distance <= 30m)
            .OrderBy(x => x.Distance)
            .Take(5)
            .Select(x => x.Mechanic)
            .ToArray();
    }

    private async Task SetStatusAsync(ServiceRequest request, string status, int? changedByUserId, string? notes, CancellationToken cancellationToken)
    {
        var oldStatusId = request.CurrentStatusId;
        var newStatusId = await GetOrCreateStatusIdAsync(status, cancellationToken);
        request.CurrentStatusId = newStatusId;
        request.AcceptedAt = status == "accepted" ? DateTime.UtcNow : request.AcceptedAt;
        request.CancelledAt = status == "cancelled" ? DateTime.UtcNow : request.CancelledAt;
        request.CompletedAt = status == "completed" ? DateTime.UtcNow : request.CompletedAt;
        db.RequestStatusHistory.Add(new RequestStatusHistory
        {
            RequestId = request.RequestId,
            OldStatusId = oldStatusId,
            NewStatusId = newStatusId,
            ChangedByUserId = changedByUserId,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<int> GetOrCreateStatusIdAsync(string statusName, CancellationToken cancellationToken)
    {
        var existingStatusId = await db.RequestStatuses
            .Where(x => x.StatusName == statusName)
            .Select(x => (int?)x.StatusId)
            .SingleOrDefaultAsync(cancellationToken);
        if (existingStatusId is not null)
        {
            return existingStatusId.Value;
        }

        var status = new RequestStatus { StatusName = statusName };
        db.RequestStatuses.Add(status);
        await db.SaveChangesAsync(cancellationToken);
        return status.StatusId;
    }

    private static string ToEmergencyStatus(string? status, int? mechanicId)
    {
        return status switch
        {
            "emergency_pending" => "EmergencyPending",
            "searching_responder" => "SearchingResponder",
            "call_connecting" => "CallConnecting",
            "call_connected" => "CallConnected",
            "accepted" when mechanicId is not null => "ResponderAssigned",
            "en_route" => "ResponderOnTheWay",
            "arrived" => "ResponderArrived",
            "in_progress" => "ServiceStarted",
            "completed" => "ServiceCompleted",
            "cancelled" => "Cancelled",
            _ => mechanicId is null ? "SearchingResponder" : "ResponderAssigned"
        };
    }

    private static string BuildStatusMessage(string status, int nearbyCount)
    {
        return status switch
        {
            "EmergencyPending" or "SearchingResponder" when nearbyCount == 0 =>
                "BikeMate saved your emergency request. No nearby responder is online yet; keep this screen open or retry shortly.",
            "EmergencyPending" or "SearchingResponder" =>
                $"BikeMate is notifying {nearbyCount} nearby responder(s).",
            "ResponderAssigned" => "A BikeMate responder accepted your emergency request.",
            "ResponderOnTheWay" => "Your responder is on the way.",
            "ResponderArrived" => "Your responder has arrived.",
            "ServiceStarted" => "Emergency service is in progress.",
            "ServiceCompleted" => "Emergency service is complete.",
            "Cancelled" => "This emergency request was cancelled.",
            "CallConnecting" or "CallConnected" => "BikeMate support call is active.",
            _ => "Emergency request is active."
        };
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

    private static string CleanNotes(string? notes)
    {
        return string.IsNullOrWhiteSpace(notes) ? "Roadside assistance needed." : notes.Trim();
    }
}
