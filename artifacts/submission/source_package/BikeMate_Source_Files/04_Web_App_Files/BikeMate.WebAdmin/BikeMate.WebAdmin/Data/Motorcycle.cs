using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class Motorcycle
{
    public int MotorcycleId { get; set; }

    public int ClientId { get; set; }

    public string Brand { get; set; } = null!;

    public string Model { get; set; } = null!;

    public int? YearModel { get; set; }

    public string? PlateNumber { get; set; }

    public string? EngineType { get; set; }

    public string? Color { get; set; }

    public string? MotorcycleImageUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
}
