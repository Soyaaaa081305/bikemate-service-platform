using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class OtpVerification
{
    public int OtpId { get; set; }

    public int UserId { get; set; }

    public string OtpHash { get; set; } = null!;

    public string Purpose { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime? ConsumedAt { get; set; }

    public int Attempts { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
