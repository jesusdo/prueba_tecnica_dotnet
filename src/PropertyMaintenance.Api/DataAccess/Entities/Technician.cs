using System;
using System.Collections.Generic;

namespace PropertyMaintenance.Api.DataAccess.Entities;

public partial class Technician
{
    public int TechnicianId { get; set; }

    public string Name { get; set; } = null!;

    public byte SpecialtyId { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();

    public virtual TechnicianSpecialty Specialty { get; set; } = null!;

    public virtual ICollection<Visit> Visits { get; set; } = new List<Visit>();
}
