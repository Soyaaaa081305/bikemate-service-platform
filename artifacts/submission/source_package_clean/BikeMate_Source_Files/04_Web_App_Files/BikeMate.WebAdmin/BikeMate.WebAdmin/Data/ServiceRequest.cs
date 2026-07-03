using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class ServiceRequest
{
    public int RequestId { get; set; }

    public int ClientId { get; set; }

    public int? ShopId { get; set; }

    public int? ShopServiceId { get; set; }

    public int? MechanicId { get; set; }

    public int CurrentStatusId { get; set; }

    public int? MotorcycleId { get; set; }

    public string IssueDescription { get; set; } = null!;

    public string? ServiceLocationAddress { get; set; }

    public decimal? ServiceLatitude { get; set; }

    public decimal? ServiceLongitude { get; set; }

    public DateTime? ScheduledAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? AcceptedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public decimal EstimatedTotal { get; set; }

    public decimal FinalTotal { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();

    public virtual RequestStatus CurrentStatus { get; set; } = null!;

    public virtual ICollection<LiveLocation> LiveLocations { get; set; } = new List<LiveLocation>();

    public virtual Mechanic? Mechanic { get; set; }

    public virtual Motorcycle? Motorcycle { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<RequestMedium> RequestMedia { get; set; } = new List<RequestMedium>();

    public virtual ICollection<RequestStatusHistory> RequestStatusHistories { get; set; } = new List<RequestStatusHistory>();

    public virtual Review? Review { get; set; }

    public virtual Shop? Shop { get; set; }

    public virtual ShopService? ShopService { get; set; }
}
