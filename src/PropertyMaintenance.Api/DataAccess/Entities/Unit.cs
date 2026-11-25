using System;
using System.Collections.Generic;

namespace PropertyMaintenance.Api.DataAccess.Entities;

public partial class Unit
{
    public int UnitId { get; set; }

    public int PropertyId { get; set; }

    public string Number { get; set; } = null!;

    public short Floor { get; set; }

    public byte UnitTypeId { get; set; }

    public virtual ICollection<Lease> Leases { get; set; } = new List<Lease>();

    public virtual ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();

    public virtual Property Property { get; set; } = null!;

    public virtual UnitType UnitType { get; set; } = null!;
}
