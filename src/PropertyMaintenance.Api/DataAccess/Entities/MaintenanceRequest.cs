using System;
using System.Collections.Generic;

namespace PropertyMaintenance.Api.DataAccess.Entities;

public partial class MaintenanceRequest
{
    public int RequestId { get; set; }

    public int UnitId { get; set; }

    public int TenantId { get; set; }

    public int? TechnicianId { get; set; }

    public string Category { get; set; } = null!;

    public byte PriorityId { get; set; }

    public byte StatusId { get; set; }

    public string Description { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual RequestPriority Priority { get; set; } = null!;

    public virtual RequestStatus Status { get; set; } = null!;

    public virtual Technician? Technician { get; set; }

    public virtual Tenant Tenant { get; set; } = null!;

    public virtual Unit Unit { get; set; } = null!;

    public virtual ICollection<Visit> Visits { get; set; } = new List<Visit>();
}
