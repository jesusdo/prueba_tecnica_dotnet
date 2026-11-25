using System;
using System.Collections.Generic;

namespace PropertyMaintenance.Api.DataAccess.Entities;

public partial class UnitType
{
    public byte UnitTypeId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public virtual ICollection<Unit> Units { get; set; } = new List<Unit>();
}
