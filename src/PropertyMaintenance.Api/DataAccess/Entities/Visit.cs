using System;
using System.Collections.Generic;

namespace PropertyMaintenance.Api.DataAccess.Entities;

public partial class Visit
{
    public int VisitId { get; set; }

    public int RequestId { get; set; }

    public int TechnicianId { get; set; }

    public DateTime VisitedAt { get; set; }

    public string? Notes { get; set; }

    public decimal Cost { get; set; }

    public virtual MaintenanceRequest Request { get; set; } = null!;

    public virtual Technician Technician { get; set; } = null!;
}
