using System;
using System.Collections.Generic;

namespace PropertyMaintenance.Api.DataAccess.Entities;

public partial class Property
{
    public int PropertyId { get; set; }

    public string Name { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string City { get; set; } = null!;

    public virtual ICollection<Unit> Units { get; set; } = new List<Unit>();
}
