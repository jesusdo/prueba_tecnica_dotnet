using Microsoft.EntityFrameworkCore;
using PropertyMaintenance.Api.DataAccess.Entities;

namespace PropertyMaintenance.Api.DataAccess.Context;

public partial class PropertyMaintenanceDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lease>().ToTable(table => table.UseSqlOutputClause(false));
        // SQL Server enforces NO ACTION. Do not null tracked foreign keys on delete.
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            foreignKey.DeleteBehavior = DeleteBehavior.NoAction;
    }
}
