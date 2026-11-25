using System;
using System.Collections.Generic;

namespace PropertyMaintenance.Api.DataAccess.Entities;

public partial class TechnicianSpecialty
{
    public byte SpecialtyId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public virtual ICollection<Technician> Technicians { get; set; } = new List<Technician>();
}
