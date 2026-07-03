using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class ShopService
{
    public int ShopServiceId { get; set; }

    public int ShopId { get; set; }

    public int CategoryId { get; set; }

    public string ServiceName { get; set; } = null!;

    public string? ServiceDescription { get; set; }

    public decimal BasePrice { get; set; }

    public int EstimatedMinutes { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ServiceCategory Category { get; set; } = null!;

    public virtual ICollection<ServiceImage> ServiceImages { get; set; } = new List<ServiceImage>();

    public virtual ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();

    public virtual Shop Shop { get; set; } = null!;
}
