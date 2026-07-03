using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class RequestMedium
{
    public int RequestMediaId { get; set; }

    public int RequestId { get; set; }

    public string MediaUrl { get; set; } = null!;

    public string MediaType { get; set; } = null!;

    public string? Caption { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ServiceRequest Request { get; set; } = null!;
}
