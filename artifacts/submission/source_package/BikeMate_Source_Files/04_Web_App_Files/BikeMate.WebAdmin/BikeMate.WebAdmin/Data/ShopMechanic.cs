using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class ShopMechanic
{
    public int ShopId { get; set; }

    public int MechanicId { get; set; }

    public DateTime AssignedAt { get; set; }

    public bool IsActive { get; set; }

    public virtual Mechanic Mechanic { get; set; } = null!;

    public virtual Shop Shop { get; set; } = null!;
}
