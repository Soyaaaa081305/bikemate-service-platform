using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class AuditLog
{
    public int AuditId { get; set; }

    public int? ActorUserId { get; set; }

    public string ActionName { get; set; } = null!;

    public string EntityName { get; set; } = null!;

    public string? EntityId { get; set; }

    public string? OldValuesJson { get; set; }

    public string? NewValuesJson { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? ActorUser { get; set; }
}
