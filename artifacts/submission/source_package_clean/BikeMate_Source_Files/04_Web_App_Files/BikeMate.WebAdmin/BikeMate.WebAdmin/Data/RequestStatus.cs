using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class RequestStatus
{
    public int StatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public virtual ICollection<RequestStatusHistory> RequestStatusHistoryNewStatuses { get; set; } = new List<RequestStatusHistory>();

    public virtual ICollection<RequestStatusHistory> RequestStatusHistoryOldStatuses { get; set; } = new List<RequestStatusHistory>();

    public virtual ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
}
