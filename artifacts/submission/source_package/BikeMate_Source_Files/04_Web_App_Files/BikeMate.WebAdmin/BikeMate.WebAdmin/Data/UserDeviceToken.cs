using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class UserDeviceToken
{
    public int DeviceTokenId { get; set; }

    public int UserId { get; set; }

    public string DeviceToken { get; set; } = null!;

    public string Platform { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
