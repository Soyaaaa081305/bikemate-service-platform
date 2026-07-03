using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class Mechanic
{
    public int MechanicId { get; set; }

    public int UserId { get; set; }

    public string? Bio { get; set; }

    public int? YearsExperience { get; set; }

    public string? CertificationImageUrl { get; set; }

    public bool IsVerified { get; set; }

    public string AvailabilityStatus { get; set; } = null!;

    public decimal? CurrentLatitude { get; set; }

    public decimal? CurrentLongitude { get; set; }

    public decimal AverageRating { get; set; }

    public int TotalCompletedJobs { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<LiveLocation> LiveLocations { get; set; } = new List<LiveLocation>();

    public virtual ICollection<MechanicAvailability> MechanicAvailabilities { get; set; } = new List<MechanicAvailability>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();

    public virtual ICollection<ShopMechanic> ShopMechanics { get; set; } = new List<ShopMechanic>();

    public virtual User User { get; set; } = null!;
}
