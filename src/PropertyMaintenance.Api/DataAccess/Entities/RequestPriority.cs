using System;
using System.Collections.Generic;

namespace PropertyMaintenance.Api.DataAccess.Entities;

public partial class RequestPriority
{
    public byte PriorityId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public virtual ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
}
