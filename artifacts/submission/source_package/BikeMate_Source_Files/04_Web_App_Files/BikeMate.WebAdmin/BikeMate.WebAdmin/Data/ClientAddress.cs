using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class ClientAddress
{
    public int AddressId { get; set; }

    public int ClientId { get; set; }

    public string? Label { get; set; }

    public string AddressLine { get; set; } = null!;

    public string? City { get; set; }

    public string? Province { get; set; }

    public string? PostalCode { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Client Client { get; set; } = null!;
}
