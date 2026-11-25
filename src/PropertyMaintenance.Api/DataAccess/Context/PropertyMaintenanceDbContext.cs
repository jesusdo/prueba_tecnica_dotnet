using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using PropertyMaintenance.Api.DataAccess.Entities;

namespace PropertyMaintenance.Api.DataAccess.Context;

public partial class PropertyMaintenanceDbContext : DbContext
{
    public PropertyMaintenanceDbContext(DbContextOptions<PropertyMaintenanceDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Lease> Leases { get; set; }

    public virtual DbSet<MaintenanceRequest> MaintenanceRequests { get; set; }

    public virtual DbSet<Property> Properties { get; set; }

    public virtual DbSet<RequestPriority> RequestPriorities { get; set; }

    public virtual DbSet<RequestStatus> RequestStatuses { get; set; }

    public virtual DbSet<Technician> Technicians { get; set; }

    public virtual DbSet<TechnicianSpecialty> TechnicianSpecialties { get; set; }

    public virtual DbSet<Tenant> Tenants { get; set; }

    public virtual DbSet<Unit> Units { get; set; }

    public virtual DbSet<UnitType> UnitTypes { get; set; }

    public virtual DbSet<Visit> Visits { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lease>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("TR_Leases_PreventOverlap"));

            entity.HasIndex(e => e.TenantId, "IX_Leases_TenantId");

            entity.HasIndex(e => new { e.UnitId, e.StartDate, e.EndDate }, "IX_Leases_UnitId_StartDate_EndDate");

            entity.Property(e => e.MonthlyRent).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Tenant).WithMany(p => p.Leases)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Leases_Tenants");

            entity.HasOne(d => d.Unit).WithMany(p => p.Leases)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Leases_Units");
        });

        modelBuilder.Entity<MaintenanceRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId);

            entity.HasIndex(e => new { e.ClosedAt, e.StatusId }, "IX_MaintenanceRequests_ClosedAt_StatusId");

            entity.HasIndex(e => new { e.TechnicianId, e.StatusId }, "IX_MaintenanceRequests_TechnicianId_StatusId");

            entity.HasIndex(e => e.TenantId, "IX_MaintenanceRequests_TenantId");

            entity.HasIndex(e => new { e.UnitId, e.CreatedAt }, "IX_MaintenanceRequests_UnitId_CreatedAt");

            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.ClosedAt).HasPrecision(3);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(CONVERT([datetime2](3),sysutcdatetime()))", "DF_MaintenanceRequests_CreatedAt");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.StatusId).HasDefaultValue((byte)1, "DF_MaintenanceRequests_StatusId");

            entity.HasOne(d => d.Priority).WithMany(p => p.MaintenanceRequests)
                .HasForeignKey(d => d.PriorityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MaintenanceRequests_Priorities");

            entity.HasOne(d => d.Status).WithMany(p => p.MaintenanceRequests)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MaintenanceRequests_Statuses");

            entity.HasOne(d => d.Technician).WithMany(p => p.MaintenanceRequests)
                .HasForeignKey(d => d.TechnicianId)
                .HasConstraintName("FK_MaintenanceRequests_Technicians");

            entity.HasOne(d => d.Tenant).WithMany(p => p.MaintenanceRequests)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MaintenanceRequests_Tenants");

            entity.HasOne(d => d.Unit).WithMany(p => p.MaintenanceRequests)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MaintenanceRequests_Units");
        });

        modelBuilder.Entity<Property>(entity =>
        {
            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<RequestPriority>(entity =>
        {
            entity.HasKey(e => e.PriorityId);

            entity.HasIndex(e => e.Code, "UQ_RequestPriorities_Code").IsUnique();

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<RequestStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId);

            entity.HasIndex(e => e.Code, "UQ_RequestStatuses_Code").IsUnique();

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Technician>(entity =>
        {
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Technicians_IsActive");
            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.Specialty).WithMany(p => p.Technicians)
                .HasForeignKey(d => d.SpecialtyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Technicians_Specialties");
        });

        modelBuilder.Entity<TechnicianSpecialty>(entity =>
        {
            entity.HasKey(e => e.SpecialtyId);

            entity.HasIndex(e => e.Code, "UQ_TechnicianSpecialties_Code").IsUnique();

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasIndex(e => e.Email, "UQ_Tenants_Email").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(320);
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(50);
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasIndex(e => new { e.PropertyId, e.Number }, "UQ_Units_PropertyId_Number").IsUnique();

            entity.Property(e => e.Number).HasMaxLength(50);

            entity.HasOne(d => d.Property).WithMany(p => p.Units)
                .HasForeignKey(d => d.PropertyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Units_Properties");

            entity.HasOne(d => d.UnitType).WithMany(p => p.Units)
                .HasForeignKey(d => d.UnitTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Units_UnitTypes");
        });

        modelBuilder.Entity<UnitType>(entity =>
        {
            entity.HasIndex(e => e.Code, "UQ_UnitTypes_Code").IsUnique();

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Visit>(entity =>
        {
            entity.HasIndex(e => e.RequestId, "IX_Visits_RequestId");

            entity.HasIndex(e => new { e.TechnicianId, e.VisitedAt }, "IX_Visits_TechnicianId_VisitedAt");

            entity.HasIndex(e => e.VisitedAt, "IX_Visits_VisitedAt");

            entity.Property(e => e.Cost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.VisitedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(CONVERT([datetime2](3),sysutcdatetime()))", "DF_Visits_VisitedAt");

            entity.HasOne(d => d.Request).WithMany(p => p.Visits)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Visits_MaintenanceRequests");

            entity.HasOne(d => d.Technician).WithMany(p => p.Visits)
                .HasForeignKey(d => d.TechnicianId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Visits_Technicians");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
