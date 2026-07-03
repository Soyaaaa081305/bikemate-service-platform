using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class ServiceCategory
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public string? IconUrl { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<ShopService> ShopServices { get; set; } = new List<ShopService>();
}
