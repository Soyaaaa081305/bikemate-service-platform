using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class PaymentStatus
{
    public int PaymentStatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
