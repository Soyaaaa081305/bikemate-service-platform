using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class Shop
{
    public int ShopId { get; set; }

    public int OwnerUserId { get; set; }

    public string ShopName { get; set; } = null!;

    public string? ShopDescription { get; set; }

    public string? AddressLine { get; set; }

    public string? City { get; set; }

    public string? Province { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string? BusinessPermitUrl { get; set; }

    public string? ShopImageUrl { get; set; }

    public string? ContactNumber { get; set; }

    public string ShopStatus { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User OwnerUser { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();

    public virtual ICollection<ShopMechanic> ShopMechanics { get; set; } = new List<ShopMechanic>();

    public virtual ICollection<ShopOperatingHour> ShopOperatingHours { get; set; } = new List<ShopOperatingHour>();

    public virtual ICollection<ShopService> ShopServices { get; set; } = new List<ShopService>();
}
