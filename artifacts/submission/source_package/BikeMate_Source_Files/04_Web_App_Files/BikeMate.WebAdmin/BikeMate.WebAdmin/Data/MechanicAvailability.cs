using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class MechanicAvailability
{
    public int AvailabilityId { get; set; }

    public int MechanicId { get; set; }

    public int DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public bool IsActive { get; set; }

    public virtual Mechanic Mechanic { get; set; } = null!;
}
