using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class RequestStatusHistory
{
    public int StatusHistoryId { get; set; }

    public int RequestId { get; set; }

    public int? OldStatusId { get; set; }

    public int NewStatusId { get; set; }

    public int? ChangedByUserId { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? ChangedByUser { get; set; }

    public virtual RequestStatus NewStatus { get; set; } = null!;

    public virtual RequestStatus? OldStatus { get; set; }

    public virtual ServiceRequest Request { get; set; } = null!;
}
