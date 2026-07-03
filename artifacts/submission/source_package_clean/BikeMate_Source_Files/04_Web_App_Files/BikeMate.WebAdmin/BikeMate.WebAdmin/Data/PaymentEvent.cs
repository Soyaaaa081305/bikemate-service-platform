using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class PaymentEvent
{
    public int PaymentEventId { get; set; }

    public int? PaymentId { get; set; }

    public string? ProviderEventId { get; set; }

    public string EventType { get; set; } = null!;

    public string PayloadJson { get; set; } = null!;

    public DateTime ReceivedAt { get; set; }

    public virtual Payment? Payment { get; set; }
}
