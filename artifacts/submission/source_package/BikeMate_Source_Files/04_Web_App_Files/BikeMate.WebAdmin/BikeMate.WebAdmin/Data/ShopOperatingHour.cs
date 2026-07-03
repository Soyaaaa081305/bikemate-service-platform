using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class ShopOperatingHour
{
    public int OperatingHourId { get; set; }

    public int ShopId { get; set; }

    public int DayOfWeek { get; set; }

    public TimeOnly OpeningTime { get; set; }

    public TimeOnly ClosingTime { get; set; }

    public bool IsClosed { get; set; }

    public virtual Shop Shop { get; set; } = null!;
}
