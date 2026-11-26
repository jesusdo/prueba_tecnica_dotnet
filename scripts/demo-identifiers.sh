#!/usr/bin/env bash
set -euo pipefail
database="${1:-${DB_NAME:-PropertyMaintenanceDb_Demo}}"
if [[ ! "$database" =~ ^[A-Za-z0-9_]{1,128}$ ]]; then
    echo "Invalid database name." >&2
    exit 1
fi
: "${MSSQL_SA_PASSWORD:?SQL Server password must be configured}"
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b -d "$database" -Q "
SET NOCOUNT ON;
SELECT TOP(1) u.PropertyId,l.UnitId,l.TenantId
FROM dbo.Leases l JOIN dbo.Units u ON u.UnitId=l.UnitId
WHERE l.StartDate<=CONVERT(date,SYSUTCDATETIME())
  AND (l.EndDate IS NULL OR l.EndDate>=CONVERT(date,SYSUTCDATETIME()))
ORDER BY l.LeaseId;
SELECT TechnicianId,Name FROM dbo.Technicians WHERE IsActive=1 ORDER BY TechnicianId;"
