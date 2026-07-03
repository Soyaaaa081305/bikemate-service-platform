using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class UserAuthProvider
{
    public int AuthProviderId { get; set; }

    public int UserId { get; set; }

    public string ProviderName { get; set; } = null!;

    public string? ProviderSubject { get; set; }

    public string? ProviderEmail { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
