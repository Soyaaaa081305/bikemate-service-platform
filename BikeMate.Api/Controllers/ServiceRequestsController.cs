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
[Route("api/service-requests")]
[Authorize]
public sealed class ServiceRequestsController(
    BikeMateDbContext db,
    IServiceRequestService serviceRequestService,
    IBookingConversationService bookingConversationService,
    IHubContext<BookingHub> bookingHub) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<ServiceRequestDto>> Create(CreateServiceRequestDto dto, CancellationToken cancellationToken)
    {
        var request = await serviceRequestService.CreateAsync(User.GetUserId(), dto, cancellationToken);
        await bookingConversationService.SyncRequestAsync(request.RequestId, cancellationToken);
        await bookingHub.Clients.Group("admin-monitoring").SendAsync("ServiceRequestCreated", request, cancellationToken);
        return Ok(request);
    }

    [HttpGet("active")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<IReadOnlyCollection<ServiceRequestDto>>> Active(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        return Ok(await serviceRequestService.QueryForDto()
            .Where(x => x.Client!.UserId == userId &&
                        x.CurrentStatus!.StatusName != "completed" &&
                        x.CurrentStatus.StatusName != "cancelled" &&
                        x.CurrentStatus.StatusName != "rejected")
            .OrderByDescending(x => x.CreatedAt)
            .Select(ServiceRequestService.ToDtoExpression())
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("my")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<IReadOnlyCollection<ServiceRequestDto>>> GetMine(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        return Ok(await serviceRequestService.QueryForDto()
            .Where(x => x.Client!.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(ServiceRequestService.ToDtoExpression())
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ServiceRequestDto>> GetById(int id, CancellationToken cancellationToken)
    {
        await EnsureCanViewAsync(id, cancellationToken);
        return Ok(await serviceRequestService.QueryForDto()
            .Where(x => x.RequestId == id)
            .Select(ServiceRequestService.ToDtoExpression())
            .SingleAsync(cancellationToken));
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = $"{AppRoles.Mechanic},{AppRoles.ShopAdmin},{AppRoles.SystemAdmin}")]
    public async Task<ActionResult<ServiceRequestDto>> UpdateStatus(int id, UpdateRequestStatusDto dto, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (User.IsInRole(AppRoles.ShopAdmin) && !User.IsInRole(AppRoles.SystemAdmin))
        {
            var ownsShopRequest = await db.ServiceRequests
                .AnyAsync(x => x.RequestId == id && x.Shop != null && x.Shop.OwnerUserId == userId, cancellationToken);
            if (!ownsShopRequest)
            {
                return Forbid();
            }
        }
        else if (!User.IsInRole(AppRoles.SystemAdmin))
        {
            var isAssignedMechanic = await db.ServiceRequests
                .AnyAsync(x => x.RequestId == id && x.Mechanic != null && x.Mechanic.UserId == userId, cancellationToken);
            if (!isAssignedMechanic)
            {
                return Forbid();
            }
        }

        var request = await serviceRequestService.UpdateStatusAsync(id, dto.Status, userId, dto.Notes, cancellationToken);
        await bookingHub.Clients.Group(BookingHub.GetRequestGroup(id)).SendAsync("ServiceStatusChanged", request, cancellationToken);
        await bookingHub.Clients.Group("admin-monitoring").SendAsync("ServiceStatusChanged", request, cancellationToken);
        return Ok(request);
    }

    [HttpPut("{id:int}/cancel")]
    public async Task<ActionResult<ServiceRequestDto>> Cancel(int id, UpdateRequestStatusDto? dto, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var requestEntity = await db.ServiceRequests
            .Include(x => x.Client)
            .Include(x => x.Shop)
            .SingleAsync(x => x.RequestId == id, cancellationToken);

        var canCancel = User.IsInRole(AppRoles.SystemAdmin) ||
            requestEntity.Client!.UserId == userId ||
            (User.IsInRole(AppRoles.ShopAdmin) && requestEntity.Shop?.OwnerUserId == userId);
        if (!canCancel)
        {
            return Forbid();
        }

        var request = await serviceRequestService.UpdateStatusAsync(id, "cancelled", userId, dto?.Notes ?? "Request cancelled.", cancellationToken);
        await bookingHub.Clients.Group(BookingHub.GetRequestGroup(id)).SendAsync("ServiceRequestCancelled", request, cancellationToken);
        await bookingHub.Clients.Group("admin-monitoring").SendAsync("ServiceRequestCancelled", request, cancellationToken);
        return Ok(request);
    }

    [HttpPut("{id:int}/assign-mechanic")]
    [Authorize(Roles = $"{AppRoles.ShopAdmin},{AppRoles.SystemAdmin}")]
    public async Task<ActionResult<ServiceRequestDto>> AssignMechanic(int id, AssignMechanicDto dto, CancellationToken cancellationToken)
    {
        var request = await db.ServiceRequests.SingleAsync(x => x.RequestId == id, cancellationToken);
        if (User.IsInRole(AppRoles.ShopAdmin) && !User.IsInRole(AppRoles.SystemAdmin))
        {
            var userId = User.GetUserId();
            var ownedShopId = await db.Shops
                .Where(x => x.OwnerUserId == userId)
                .Select(x => (int?)x.ShopId)
                .FirstOrDefaultAsync(cancellationToken);
            if (ownedShopId is null || request.ShopId != ownedShopId)
            {
                return Forbid();
            }

            var mechanicBelongsToShop = await db.ShopMechanics.AnyAsync(
                x => x.ShopId == ownedShopId && x.MechanicId == dto.MechanicId && x.IsActive,
                cancellationToken);
            if (!mechanicBelongsToShop)
            {
                return BadRequest(new { error = "Select a mechanic assigned to your shop." });
            }
        }

        request.MechanicId = dto.MechanicId;
        await db.SaveChangesAsync(cancellationToken);
        var updated = await serviceRequestService.UpdateStatusAsync(id, "accepted", User.GetUserId(), "Mechanic assigned.", cancellationToken);
        await bookingConversationService.SyncRequestAsync(id, cancellationToken);
        await bookingHub.Clients.Group(BookingHub.GetRequestGroup(id)).SendAsync("ServiceStatusChanged", updated, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("{id:int}/media")]
    public async Task<IActionResult> AddMedia(int id, UploadMediaDto dto, CancellationToken cancellationToken)
    {
        db.RequestMedia.Add(new RequestMedia
        {
            RequestId = id,
            MediaUrl = dto.MediaUrl,
            MediaType = dto.MediaType,
            Caption = dto.Caption,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Media attached." });
    }

    [HttpPut("{id:int}/select-shop")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<ServiceRequestDto>> SelectShop(int id, SelectShopDto dto, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var request = await db.ServiceRequests
            .Include(x => x.Client)
            .Include(x => x.LineItems)
            .SingleAsync(x => x.RequestId == id && x.Client!.UserId == userId, cancellationToken);
        var shop = await db.Shops
            .Where(x => x.ShopId == dto.ShopId && x.ShopStatus == "verified")
            .Select(x => new ShopAvailability(x.AllowsReservations, x.AllowsPickup, x.AllowsOnsiteRepair))
            .SingleOrDefaultAsync(cancellationToken);
        if (shop is null)
        {
            return BadRequest(new { error = "Select an available repair shop." });
        }

        var availabilityError = ShopAvailabilityError(shop, request.ScheduledAt, request.IssueDescription);
        if (availabilityError is not null)
        {
            return BadRequest(new { error = availabilityError });
        }

        var serviceIds = ServiceRequestService.MergeIds(dto.ShopServiceId, dto.ShopServiceIds);
        var servicesQuery = db.ShopServices
            .Where(x => x.ShopId == dto.ShopId && x.IsActive);
        if (serviceIds.Count > 0)
        {
            servicesQuery = servicesQuery.Where(x => serviceIds.Contains(x.ShopServiceId));
        }

        var services = await servicesQuery
            .OrderBy(x => x.ServiceName)
            .ToListAsync(cancellationToken);
        if (services.Count == 0)
        {
            return BadRequest(new { error = "Select an available service from this shop." });
        }

        if (serviceIds.Count > 0 && services.Count != serviceIds.Count)
        {
            return BadRequest(new { error = "One or more selected services are no longer available from this shop." });
        }

        var productIds = ServiceRequestService.MergeIds(dto.ProductId, dto.ProductIds);
        var selectedProducts = productIds.Count == 0
            ? []
            : await db.Products
                .Where(x => productIds.Contains(x.ProductId) && x.ShopId == dto.ShopId && x.IsActive)
                .OrderBy(x => x.ProductName)
                .ToListAsync(cancellationToken);
        if (productIds.Count > 0 && selectedProducts.Count != productIds.Count)
        {
            return BadRequest(new { error = "One or more selected products are no longer available from this shop." });
        }

        if (selectedProducts.Any(x => x.StockQuantity <= 0))
        {
            return BadRequest(new { error = "One or more selected products are currently out of stock." });
        }

        var primaryService = services.First();
        request.ShopId = dto.ShopId;
        request.ShopServiceId = primaryService.ShopServiceId;
        request.EstimatedTotal = services.Sum(x => x.BasePrice) + selectedProducts.Sum(x => x.Price);
        if (request.LineItems.Count > 0)
        {
            db.ServiceRequestLineItems.RemoveRange(request.LineItems);
            request.LineItems.Clear();
        }

        foreach (var service in services)
        {
            request.LineItems.Add(new ServiceRequestLineItem
            {
                ItemType = "service",
                ShopServiceId = service.ShopServiceId,
                ItemName = service.ServiceName,
                Quantity = 1,
                UnitPrice = service.BasePrice,
                LineTotal = service.BasePrice,
                CreatedAt = DateTime.UtcNow
            });
        }

        foreach (var product in selectedProducts)
        {
            request.LineItems.Add(new ServiceRequestLineItem
            {
                ItemType = "product",
                ProductId = product.ProductId,
                ItemName = product.ProductName,
                Quantity = 1,
                UnitPrice = product.Price,
                LineTotal = product.Price,
                CreatedAt = DateTime.UtcNow
            });
        }

        var mechanicId = await db.ShopMechanics
            .Where(x => x.ShopId == dto.ShopId && x.IsActive)
            .Select(x => (int?)x.MechanicId)
            .FirstOrDefaultAsync(cancellationToken);
        request.MechanicId = mechanicId ?? request.MechanicId;
        if (request.MechanicId is not null)
        {
            await EnsureInitialMechanicLocationAsync(request, request.MechanicId.Value, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        var updated = await serviceRequestService.UpdateStatusAsync(id, request.MechanicId is null ? "pending" : "accepted", userId, "Customer selected repair shop.", cancellationToken);
        await bookingConversationService.SyncRequestAsync(id, cancellationToken);
        await bookingHub.Clients.Group(BookingHub.GetRequestGroup(id)).SendAsync("ServiceRequestAccepted", updated, cancellationToken);
        return Ok(updated);
    }

    [HttpGet("{id:int}/timeline")]
    public async Task<IActionResult> Timeline(int id, CancellationToken cancellationToken)
    {
        await EnsureCanViewAsync(id, cancellationToken);
        return Ok(await db.RequestStatusHistory
            .Include(x => x.OldStatus)
            .Include(x => x.NewStatus)
            .Include(x => x.ChangedByUser)
            .Where(x => x.RequestId == id)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new
            {
                x.StatusHistoryId,
                x.RequestId,
                OldStatus = x.OldStatus == null ? null : x.OldStatus.StatusName,
                NewStatus = x.NewStatus!.StatusName,
                ChangedBy = x.ChangedByUser == null ? null : x.ChangedByUser.FirstName + " " + x.ChangedByUser.LastName,
                x.Notes,
                x.CreatedAt
            })
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("upcoming")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<IReadOnlyCollection<ServiceRequestDto>>> Upcoming(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        return Ok(await serviceRequestService.QueryForDto()
            .Where(x => x.Client!.UserId == userId && x.ScheduledAt >= DateTime.UtcNow && x.CurrentStatus!.StatusName != "completed" && x.CurrentStatus.StatusName != "cancelled")
            .OrderBy(x => x.ScheduledAt)
            .Select(ServiceRequestService.ToDtoExpression())
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("history")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<IReadOnlyCollection<ServiceRequestDto>>> History(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        return Ok(await serviceRequestService.QueryForDto()
            .Where(x => x.Client!.UserId == userId && (x.CurrentStatus!.StatusName == "completed" || x.CurrentStatus.StatusName == "cancelled"))
            .OrderByDescending(x => x.CreatedAt)
            .Select(ServiceRequestService.ToDtoExpression())
            .ToArrayAsync(cancellationToken));
    }

    private static string? ShopAvailabilityError(ShopAvailability shop, DateTime? scheduledAt, string issueDescription)
    {
        if (scheduledAt is not null)
        {
            return shop.AllowsReservations ? null : "This shop is not accepting reservations right now.";
        }

        if (issueDescription.Contains("Assistance method: Pick-up Repair", StringComparison.OrdinalIgnoreCase))
        {
            return shop.AllowsPickup ? null : "This shop is not accepting pickup repair right now.";
        }

        return shop.AllowsOnsiteRepair ? null : "This shop is not accepting on-site repair right now.";
    }

    private sealed record ShopAvailability(bool AllowsReservations, bool AllowsPickup, bool AllowsOnsiteRepair);

    private async Task EnsureCanViewAsync(int requestId, CancellationToken cancellationToken)
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
            throw new UnauthorizedAccessException("You cannot view this request.");
        }
    }

    private async Task EnsureInitialMechanicLocationAsync(ServiceRequest request, int mechanicId, CancellationToken cancellationToken)
    {
        var hasLocation = await db.LiveLocations.AnyAsync(x => x.RequestId == request.RequestId, cancellationToken);
        if (hasLocation)
        {
            return;
        }

        var mechanic = await db.Mechanics.SingleOrDefaultAsync(x => x.MechanicId == mechanicId, cancellationToken);
        if (mechanic is null)
        {
            return;
        }

        var latitude = request.ServiceLatitude is null ? mechanic.CurrentLatitude ?? 14.6010m : request.ServiceLatitude.Value + 0.0030m;
        var longitude = request.ServiceLongitude is null ? mechanic.CurrentLongitude ?? 120.9830m : request.ServiceLongitude.Value - 0.0020m;

        mechanic.AvailabilityStatus = "online";
        mechanic.CurrentLatitude = latitude;
        mechanic.CurrentLongitude = longitude;
        mechanic.UpdatedAt = DateTime.UtcNow;

        db.LiveLocations.Add(new LiveLocation
        {
            RequestId = request.RequestId,
            MechanicId = mechanicId,
            Latitude = latitude,
            Longitude = longitude,
            AccuracyMeters = 12m,
            CreatedAt = DateTime.UtcNow
        });
    }
}
