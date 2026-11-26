#!/usr/bin/env bash
set -euo pipefail
export LC_ALL=C

if (( $# > 1 )); then
    echo "Usage: bash validate-sql.sh [database_name]" >&2
    exit 2
fi
database_name="${1-${TEST_DATABASE_NAME:-PropertyMaintenanceDb_T04_Integration}}"
if [[ ! "$database_name" =~ ^[A-Za-z0-9_]+$ ]] || (( ${#database_name} > 128 )); then
    echo "Database name must contain 1-128 ASCII letters, digits or underscores." >&2
    exit 2
fi
if [[ -z "${MSSQL_SA_PASSWORD:-}" ]]; then
    echo "MSSQL_SA_PASSWORD is required." >&2
    exit 2
fi
export SQLCMDPASSWORD="$MSSQL_SA_PASSWORD"

# Reports run last. Their known specification failure must remain a failure.
for validator in ValidateSchema ValidateConstraints ValidateLeaseOverlap ValidateSeed ValidateReports; do
    echo "Running $validator"
    /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -d "$database_name" \
        -i "/opt/propertymaintenance/database/validation/$validator.sql"
done
