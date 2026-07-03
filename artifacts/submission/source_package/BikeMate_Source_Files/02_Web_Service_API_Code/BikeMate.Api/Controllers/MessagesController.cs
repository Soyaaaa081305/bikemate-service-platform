using BikeMate.Api.Helpers;
using BikeMate.Api.Hubs;
using BikeMate.Api.Services;
using BikeMate.Core.DTOs;
using BikeMate.Core.Entities;
using BikeMate.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BikeMate.Api.Controllers;

[ApiController]
[Route("api/conversations")]
[Authorize]
public sealed class MessagesController(
    BikeMateDbContext db,
    IMessageService messageService,
    IBookingConversationService bookingConversationService,
    IHubContext<ChatHub> hubContext,
    ILogger<MessagesController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ConversationSummaryDto>>> GetConversations(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        try
        {
            await bookingConversationService.SyncForUserAsync(userId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Conversation sync failed for user {UserId}; returning existing conversations.", userId);
        }

        var conversations = await db.Conversations
            .Include(x => x.Participants)
            .ThenInclude(x => x.User)
            .Include(x => x.Messages)
            .Include(x => x.Request)
            .ThenInclude(x => x!.Shop)
            .Include(x => x.Request)
            .ThenInclude(x => x!.ShopService)
            .Include(x => x.Request)
            .ThenInclude(x => x!.Mechanic)
            .ThenInclude(x => x!.User)
            .Include(x => x.Request)
            .ThenInclude(x => x!.CurrentStatus)
            .Where(x =>
                x.Participants.Any(p => p.UserId == userId) &&
                !(x.Request != null &&
                  x.Request.IssueDescription.StartsWith("[EMERGENCY]") &&
                  x.ConversationType != "emergency_support" &&
                  x.ConversationType != "emergency_request"))
            .ToArrayAsync(cancellationToken);

        return Ok(conversations
            .OrderByDescending(ConversationSortTime)
            .Select(x => ToSummary(x, userId))
            .ToArray());
    }

    [HttpPost]
    [HttpPost("start")]
    public async Task<ActionResult<ConversationDto>> Start(StartConversationDto dto, CancellationToken cancellationToken)
    {
        var participants = dto.ParticipantUserIds.Append(User.GetUserId()).Distinct().ToArray();
        var conversation = new Conversation
        {
            RequestId = dto.RequestId,
            ConversationType = dto.RequestId is null ? "direct" : "service_request",
            CreatedAt = DateTime.UtcNow,
            Participants = participants.Select(userId => new ConversationParticipant { UserId = userId, JoinedAt = DateTime.UtcNow }).ToList()
        };
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new ConversationDto(conversation.ConversationId, conversation.RequestId, conversation.ConversationType, conversation.LastMessageAt));
    }

    [HttpGet("{conversationId:int}")]
    public async Task<ActionResult<ConversationSummaryDto>> GetConversation(int conversationId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var conversation = await db.Conversations
            .Include(x => x.Participants).ThenInclude(x => x.User)
            .Include(x => x.Messages)
            .Include(x => x.Request).ThenInclude(x => x!.Shop)
            .Include(x => x.Request).ThenInclude(x => x!.ShopService)
            .Include(x => x.Request).ThenInclude(x => x!.Mechanic).ThenInclude(x => x!.User)
            .Include(x => x.Request).ThenInclude(x => x!.CurrentStatus)
            .SingleOrDefaultAsync(x => x.ConversationId == conversationId && x.Participants.Any(p => p.UserId == userId), cancellationToken);
        if (conversation is null)
        {
            return NotFound(new { error = "Conversation not found." });
        }

        return Ok(ToSummary(conversation, userId));
    }

    [HttpGet("{conversationId:int}/messages")]
    public async Task<ActionResult<IReadOnlyCollection<MessageDto>>> GetMessages(int conversationId, CancellationToken cancellationToken)
    {
        var canView = await db.ConversationParticipants
            .AnyAsync(x => x.ConversationId == conversationId && x.UserId == User.GetUserId(), cancellationToken);
        if (!canView)
        {
            return Forbid();
        }

        return Ok(await db.Messages
            .Where(x => x.ConversationId == conversationId)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new MessageDto(x.MessageId, x.ConversationId, x.SenderUserId, x.MessageText, x.AttachmentUrl, x.CreatedAt, x.ReadAt))
            .ToArrayAsync(cancellationToken));
    }

    [HttpPost("{conversationId:int}/messages")]
    public async Task<ActionResult<MessageDto>> Send(int conversationId, SendMessageDto dto, CancellationToken cancellationToken)
    {
        var message = await messageService.SendAsync(User.GetUserId(), conversationId, dto, cancellationToken);
        await hubContext.Clients.Group($"conversation:{conversationId}").SendAsync("MessageReceived", message, cancellationToken);
        return Ok(message);
    }

    [HttpPost("{conversationId:int}/read")]
    [HttpPut("{conversationId:int}/read-all")]
    public async Task<IActionResult> MarkRead(int conversationId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        await db.Messages
            .Where(x => x.ConversationId == conversationId && x.SenderUserId != userId && x.ReadAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(m => m.ReadAt, DateTime.UtcNow), cancellationToken);

        var participant = await db.ConversationParticipants.SingleOrDefaultAsync(x => x.ConversationId == conversationId && x.UserId == userId, cancellationToken);
        if (participant is not null)
        {
            participant.LastReadAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(new { message = "Conversation marked as read." });
    }

    [HttpPut("/api/messages/{messageId:int}/read")]
    public async Task<IActionResult> MarkMessageRead(int messageId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var message = await db.Messages
            .Include(x => x.Conversation).ThenInclude(x => x!.Participants)
            .SingleAsync(x => x.MessageId == messageId && x.Conversation!.Participants.Any(p => p.UserId == userId), cancellationToken);
        message.ReadAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await hubContext.Clients.Group($"conversation:{message.ConversationId}").SendAsync("MessageRead", messageId, cancellationToken);
        return Ok(new { message = "Message marked as read." });
    }

    private static ConversationSummaryDto ToSummary(Conversation conversation, int userId)
    {
        var otherUser = conversation.Participants.FirstOrDefault(p => p.UserId != userId)?.User;
        var lastMessage = conversation.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
        var isEmergency = conversation.ConversationType is "emergency_support" or "emergency_request";
        var isShop = conversation.ConversationType == "booking_shop";
        var isMechanic = conversation.ConversationType == "booking_mechanic";
        var isShopOwner = isShop && conversation.Request?.Shop?.OwnerUserId == userId;
        var isAssignedMechanic = isMechanic && conversation.Request?.Mechanic?.UserId == userId;
        var title = isEmergency
            ? "BikeMate Emergency"
            : isShop
            ? isShopOwner ? FullName(otherUser) : conversation.Request?.Shop?.ShopName
            : isMechanic
                ? isAssignedMechanic ? FullName(otherUser) : FullName(conversation.Request?.Mechanic?.User ?? otherUser)
                : FullName(otherUser);
        var partnerType = isEmergency
            ? "Emergency support"
            : isShop
                ? isShopOwner ? "Customer" : "Shop"
                : isMechanic
                    ? isAssignedMechanic ? "Customer" : "Assigned mechanic"
                    : "BikeMate contact";
        var service = conversation.Request?.ShopService?.ServiceName
            ?? conversation.Request?.IssueDescription
            ?? "Booking support";
        if (isEmergency)
        {
            service = service.Replace("[EMERGENCY]", "", StringComparison.OrdinalIgnoreCase).Trim();
        }
        var bookingReference = conversation.RequestId is null ? null : $"BM-{conversation.RequestId:000000}";
        var subtitle = bookingReference is null
            ? otherUser?.PhoneNumber ?? otherUser?.Email
            : $"{partnerType} | {service} | {bookingReference}";

        return new ConversationSummaryDto(
            conversation.ConversationId,
            conversation.RequestId,
            conversation.ConversationType,
            conversation.LastMessageAt,
            string.IsNullOrWhiteSpace(title) ? $"Conversation #{conversation.ConversationId}" : title,
            subtitle,
            otherUser?.UserId,
            otherUser?.ProfileImageUrl,
            lastMessage?.MessageText,
            conversation.Messages.Count(x => x.SenderUserId != userId && x.ReadAt == null),
            conversation.Request?.CurrentStatus?.StatusName,
            conversation.Request?.ScheduledAt);
    }

    private static string? FullName(User? user)
    {
        return user is null ? null : $"{user.FirstName} {user.LastName}".Trim();
    }

    private static DateTime ConversationSortTime(Conversation conversation)
    {
        var onlyMessage = conversation.Messages.Count == 1 ? conversation.Messages[0] : null;
        var isAutomatedOnly = onlyMessage is not null &&
            (onlyMessage.MessageText.StartsWith("Booking BM-", StringComparison.Ordinal) ||
             onlyMessage.MessageText.StartsWith("BikeMate Emergency received", StringComparison.Ordinal) ||
             onlyMessage.MessageText.StartsWith("Hi ", StringComparison.Ordinal));
        return isAutomatedOnly
            ? conversation.Request?.CreatedAt ?? conversation.CreatedAt
            : conversation.LastMessageAt ?? conversation.CreatedAt;
    }
}
