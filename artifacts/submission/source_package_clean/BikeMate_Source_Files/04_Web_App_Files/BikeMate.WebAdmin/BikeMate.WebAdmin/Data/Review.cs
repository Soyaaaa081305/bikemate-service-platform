using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class Review
{
    public int ReviewId { get; set; }

    public int RequestId { get; set; }

    public int ClientId { get; set; }

    public int MechanicId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual Mechanic Mechanic { get; set; } = null!;

    public virtual ServiceRequest Request { get; set; } = null!;
}
