using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class ServiceImage
{
    public int ServiceImageId { get; set; }

    public int ShopServiceId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ShopService ShopService { get; set; } = null!;
}
