using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class Notification
{
    public int NotificationId { get; set; }

    public int UserId { get; set; }

    public string? NotificationType { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string? DataJson { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
