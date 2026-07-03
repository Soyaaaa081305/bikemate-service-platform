using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BikeMate.Api.Helpers;
using BikeMate.Api.Services;
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
public sealed class PaymentsController(
    BikeMateDbContext db,
    IPaymentService paymentService,
    IPayMongoService payMongoService,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("create-checkout-session")]
    [HttpPost("create-checkout")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<PaymentDto>> CreateCheckout(CreateCheckoutSessionDto dto, CancellationToken cancellationToken)
    {
        return Ok(await paymentService.CreateCheckoutAsync(User.GetUserId(), dto, cancellationToken));
    }

    [HttpPost("paymongo-webhook")]
    [HttpPost("webhook/paymongo")]
    [AllowAnonymous]
    public async Task<IActionResult> PayMongoWebhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var payloadJson = await reader.ReadToEndAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return BadRequest(new { error = "Empty webhook payload." });
        }

        var webhookSecret = configuration["PayMongo:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(webhookSecret) ||
            webhookSecret.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "PayMongo webhook secret is not configured." });
        }

        var signature = Request.Headers["Paymongo-Signature"].FirstOrDefault();
        if (!IsValidPayMongoSignature(payloadJson, webhookSecret, signature))
        {
            return Unauthorized(new { error = "Invalid PayMongo signature." });
        }

        using var document = JsonDocument.Parse(payloadJson);
        var root = document.RootElement;
        var eventType = TryGetString(root, "type")
            ?? TryGetNestedString(root, "data", "attributes", "type")
            ?? "paymongo.webhook.received";
        var providerEventId = TryGetNestedString(root, "data", "id") ?? TryGetString(root, "id");
        var referenceNumber = FindString(root, "reference_number", "external_reference_number");
        var providerCheckoutId = FindString(root, "checkout_session_id", "checkout_session");
        var status = FindString(root, "status");
        var paid = eventType.Contains("paid", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "paid", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase);

        Payment? payment = null;
        if (!string.IsNullOrWhiteSpace(referenceNumber) || !string.IsNullOrWhiteSpace(providerCheckoutId))
        {
            payment = await db.Payments
                .Include(x => x.PaymentStatus)
                .Include(x => x.Request).ThenInclude(x => x!.CurrentStatus)
                .FirstOrDefaultAsync(x =>
                    x.ProviderReferenceNumber == referenceNumber ||
                    x.ProviderCheckoutSessionId == providerCheckoutId,
                    cancellationToken);
        }

        if (paid && payment is not null)
        {
            await MarkPaymentPaidAsync(payment, FindString(root, "payment_id"), "PayMongo webhook confirmed payment.", cancellationToken);
        }

        db.PaymentEvents.Add(new PaymentEvent
        {
            PaymentId = payment?.PaymentId,
            ProviderEventId = providerEventId,
            EventType = eventType,
            PayloadJson = payloadJson,
            ReceivedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { received = true });
    }

    [HttpGet("request/{requestId:int}")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyCollection<PaymentDto>>> GetForRequest(int requestId, CancellationToken cancellationToken)
    {
        if (!await CanViewRequestPaymentsAsync(requestId, cancellationToken))
        {
            return Forbid();
        }

        return Ok(await QueryPayments()
            .Where(x => x.RequestId == requestId)
            .Select(x => ToDto(x))
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("{paymentId:int}")]
    [Authorize]
    public async Task<ActionResult<PaymentDto>> GetById(int paymentId, CancellationToken cancellationToken)
    {
        var payment = await QueryPayments()
            .Where(x => x.PaymentId == paymentId)
            .SingleOrDefaultAsync(cancellationToken);
        if (payment is null)
        {
            return NotFound();
        }

        if (!await CanViewRequestPaymentsAsync(payment.RequestId, cancellationToken))
        {
            return Forbid();
        }

        return Ok(ToDto(payment));
    }

    [HttpGet("{paymentId:int}/status")]
    [Authorize]
    public async Task<ActionResult<PaymentDto>> GetStatus(int paymentId, CancellationToken cancellationToken)
    {
        return await GetById(paymentId, cancellationToken);
    }

    [HttpPost("{paymentId:int}/refresh")]
    [Authorize]
    public async Task<ActionResult<PaymentDto>> RefreshPayment(int paymentId, CancellationToken cancellationToken)
    {
        var payment = await db.Payments
            .Include(x => x.Client)
            .Include(x => x.PaymentStatus)
            .Include(x => x.Request).ThenInclude(x => x!.CurrentStatus)
            .SingleAsync(x => x.PaymentId == paymentId, cancellationToken);

        if (payment.Client?.UserId != User.GetUserId() && !User.IsInRole(AppRoles.SystemAdmin))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(payment.ProviderCheckoutSessionId))
        {
            return Ok(ToDto(payment));
        }

        var checkoutStatus = await payMongoService.GetCheckoutStatusAsync(payment.ProviderCheckoutSessionId, cancellationToken);
        if (checkoutStatus.IsPaid)
        {
            await MarkPaymentPaidAsync(payment, checkoutStatus.ProviderPaymentId, "PayMongo checkout refresh confirmed payment.", cancellationToken);
        }

        db.PaymentEvents.Add(new PaymentEvent
        {
            PaymentId = payment.PaymentId,
            ProviderEventId = $"manual-refresh-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            EventType = checkoutStatus.IsPaid ? "paymongo.checkout.paid.refresh" : $"paymongo.checkout.{checkoutStatus.Status ?? "unknown"}.refresh",
            PayloadJson = checkoutStatus.PayloadJson ?? "{}",
            ReceivedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(payment).Reference(x => x.PaymentStatus).LoadAsync(cancellationToken);
        return Ok(ToDto(payment));
    }

    [HttpGet("request/{requestId:int}/latest")]
    [Authorize]
    public async Task<ActionResult<PaymentDto>> LatestForRequest(int requestId, CancellationToken cancellationToken)
    {
        if (!await CanViewRequestPaymentsAsync(requestId, cancellationToken))
        {
            return Forbid();
        }

        var payment = await QueryPayments()
            .Where(x => x.RequestId == requestId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => ToDto(x))
            .FirstOrDefaultAsync(cancellationToken);
        return payment is null ? NotFound() : Ok(payment);
    }

    [HttpGet("history")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<IReadOnlyCollection<PaymentDto>>> History(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        return Ok(await QueryPayments()
            .Where(x => x.Client!.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => ToDto(x))
            .ToArrayAsync(cancellationToken));
    }

    private IQueryable<Payment> QueryPayments()
    {
        return db.Payments.Include(x => x.PaymentStatus).Include(x => x.Client);
    }

    private async Task<bool> CanViewRequestPaymentsAsync(int requestId, CancellationToken cancellationToken)
    {
        if (User.IsInRole(AppRoles.SystemAdmin))
        {
            return true;
        }

        var userId = User.GetUserId();
        return await db.ServiceRequests.AnyAsync(x =>
            x.RequestId == requestId &&
            ((x.Client != null && x.Client.UserId == userId) ||
             (x.Mechanic != null && x.Mechanic.UserId == userId) ||
             (x.Shop != null && x.Shop.OwnerUserId == userId)),
            cancellationToken);
    }

    private static PaymentDto ToDto(Payment x)
    {
        return x.ToDto();
    }

    private async Task MarkPaymentPaidAsync(Payment payment, string? providerPaymentId, string requestNote, CancellationToken cancellationToken)
    {
        var wasAlreadyPaid = string.Equals(payment.PaymentStatus?.StatusName, PaymentStatuses.Paid, StringComparison.OrdinalIgnoreCase);
        payment.PaymentStatusId = await db.PaymentStatuses
            .Where(x => x.StatusName == PaymentStatuses.Paid)
            .Select(x => x.PaymentStatusId)
            .SingleAsync(cancellationToken);
        payment.PaidAt ??= DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;
        payment.ProviderPaymentId ??= providerPaymentId;

        if (!wasAlreadyPaid && payment.Request is not null)
        {
            await MarkRequestPaidAsync(payment.Request, payment.Amount, requestNote, cancellationToken);
        }
    }

    private async Task MarkRequestPaidAsync(ServiceRequest request, decimal paidAmount, string note, CancellationToken cancellationToken)
    {
        if (string.Equals(request.CurrentStatus?.StatusName, RequestStatuses.Paid, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var oldStatusId = request.CurrentStatusId;
        var paidStatusId = await GetOrCreateRequestStatusIdAsync(RequestStatuses.Paid, cancellationToken);
        request.CurrentStatusId = paidStatusId;
        if (request.FinalTotal <= 0)
        {
            request.FinalTotal = paidAmount;
        }

        db.RequestStatusHistory.Add(new RequestStatusHistory
        {
            RequestId = request.RequestId,
            OldStatusId = oldStatusId,
            NewStatusId = paidStatusId,
            Notes = note,
            CreatedAt = DateTime.UtcNow
        });
    }

    private async Task<int> GetOrCreateRequestStatusIdAsync(string statusName, CancellationToken cancellationToken)
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

    private static bool IsValidPayMongoSignature(string payloadJson, string secret, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        var signatures = ParsePayMongoSignature(signatureHeader);
        if (!signatures.TryGetValue("t", out var timestamp) || string.IsNullOrWhiteSpace(timestamp))
        {
            return false;
        }

        var modeSignature = signatures.TryGetValue("te", out var testSignature) && !string.IsNullOrWhiteSpace(testSignature)
            ? testSignature
            : signatures.TryGetValue("li", out var liveSignature) && !string.IsNullOrWhiteSpace(liveSignature)
                ? liveSignature
                : null;
        if (string.IsNullOrWhiteSpace(modeSignature))
        {
            return false;
        }

        var signedPayload = $"{timestamp}.{payloadJson}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload))).ToLowerInvariant();
        return FixedTimeEquals(expected, modeSignature);
    }

    private static Dictionary<string, string> ParsePayMongoSignature(string signatureHeader)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var piece in signatureHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var equalsIndex = piece.IndexOf('=', StringComparison.Ordinal);
            if (equalsIndex >= 0 && equalsIndex < piece.Length - 1)
            {
                values[piece[..equalsIndex]] = piece[(equalsIndex + 1)..];
            }
        }

        return values;
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(left),
            Encoding.UTF8.GetBytes(right.ToLowerInvariant()));
    }

    private static string? TryGetNestedString(JsonElement root, params string[] path)
    {
        var current = root;
        foreach (var part in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(part, out current))
            {
                return null;
            }
        }

        return current.ValueKind == JsonValueKind.String ? current.GetString() : current.ToString();
    }

    private static string? TryGetString(JsonElement root, string name)
    {
        return root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var value)
            ? value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString()
            : null;
    }

    private static string? FindString(JsonElement element, params string[] names)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (names.Any(name => string.Equals(name, property.Name, StringComparison.OrdinalIgnoreCase)) &&
                    property.Value.ValueKind is JsonValueKind.String or JsonValueKind.Number)
                {
                    return property.Value.ToString();
                }

                var nested = FindString(property.Value, names);
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = FindString(item, names);
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }
        }

        return null;
    }
}
