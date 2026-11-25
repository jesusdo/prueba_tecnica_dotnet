#!/usr/bin/env bash
set -euo pipefail
# Run from the repository root inside dev. Configuration stays outside source.
: "${ConnectionStrings__PropertyMaintenance:?Set the local DbUp-initialized SQL connection}"
dotnet tool restore
dotnet ef dbcontext scaffold "$ConnectionStrings__PropertyMaintenance" Microsoft.EntityFrameworkCore.SqlServer \
  --project src/PropertyMaintenance.Api \
  --context PropertyMaintenanceDbContext --context-dir DataAccess/Context \
  --output-dir DataAccess/Entities \
  --namespace PropertyMaintenance.Api.DataAccess.Entities \
  --context-namespace PropertyMaintenance.Api.DataAccess.Context --no-onconfiguring \
  --table dbo.Properties --table dbo.Units --table dbo.Tenants --table dbo.Leases \
  --table dbo.Technicians --table dbo.MaintenanceRequests --table dbo.Visits \
  --table dbo.UnitTypes --table dbo.TechnicianSpecialties \
  --table dbo.RequestPriorities --table dbo.RequestStatuses "$@"
