using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int RequestId { get; set; }

    public int ClientId { get; set; }

    public int PaymentStatusId { get; set; }

    public int? PaymentMethodId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = null!;

    public string ProviderName { get; set; } = null!;

    public string? ProviderCheckoutSessionId { get; set; }

    public string? ProviderPaymentId { get; set; }

    public string? ProviderReferenceNumber { get; set; }

    public string? CheckoutUrl { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual ICollection<PaymentEvent> PaymentEvents { get; set; } = new List<PaymentEvent>();

    public virtual PaymentMethod? PaymentMethod { get; set; }

    public virtual PaymentStatus PaymentStatus { get; set; } = null!;

    public virtual ServiceRequest Request { get; set; } = null!;
}
