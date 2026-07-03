using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class Client
{
    public int ClientId { get; set; }

    public int UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ClientAddress> ClientAddresses { get; set; } = new List<ClientAddress>();

    public virtual ICollection<Motorcycle> Motorcycles { get; set; } = new List<Motorcycle>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();

    public virtual User User { get; set; } = null!;
}
