using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class LiveLocation
{
    public int LiveLocationId { get; set; }

    public int? RequestId { get; set; }

    public int? MechanicId { get; set; }

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public decimal? AccuracyMeters { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Mechanic? Mechanic { get; set; }

    public virtual ServiceRequest? Request { get; set; }
}
