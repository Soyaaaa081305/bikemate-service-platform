using BikeMate.Core.DTOs;
using BikeMate.Core.Entities;

namespace BikeMate.Api.Helpers;

public static class PaymentMappingExtensions
{
    public static PaymentDto ToDto(this Payment payment)
    {
        return new PaymentDto(
            payment.PaymentId,
            payment.RequestId,
            payment.PaymentStatus!.StatusName,
            payment.Amount,
            payment.Currency,
            payment.ProviderName,
            payment.CheckoutUrl,
            payment.ProviderReferenceNumber,
            payment.CreatedAt,
            payment.PaidAt);
    }

    public static PaymentDto ToDto(this Payment payment, string statusOverride)
    {
        return new PaymentDto(
            payment.PaymentId,
            payment.RequestId,
            statusOverride,
            payment.Amount,
            payment.Currency,
            payment.ProviderName,
            payment.CheckoutUrl,
            payment.ProviderReferenceNumber,
            payment.CreatedAt,
            payment.PaidAt);
    }
}
