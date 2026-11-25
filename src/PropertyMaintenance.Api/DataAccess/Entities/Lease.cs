using System;
using System.Collections.Generic;

namespace PropertyMaintenance.Api.DataAccess.Entities;

public partial class Lease
{
    public int LeaseId { get; set; }

    public int UnitId { get; set; }

    public int TenantId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public decimal MonthlyRent { get; set; }

    public virtual Tenant Tenant { get; set; } = null!;

    public virtual Unit Unit { get; set; } = null!;
}
