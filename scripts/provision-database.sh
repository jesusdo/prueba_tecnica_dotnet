#!/usr/bin/env bash
set -euo pipefail
export LC_ALL=C

if (( $# > 1 )); then
    echo "Usage: bash provision-database.sh [database_name]" >&2
    exit 2
fi
database_name="${1-${DB_NAME:-PropertyMaintenanceDb_Demo}}"
if [[ ! "$database_name" =~ ^[A-Za-z0-9_]+$ ]] || (( ${#database_name} > 128 )); then
    echo "Database name must contain 1-128 ASCII letters, digits or underscores." >&2
    exit 2
fi
if [[ -z "${MSSQL_SA_PASSWORD:-}" ]]; then
    echo "MSSQL_SA_PASSWORD is required." >&2
    exit 2
fi
export SQLCMDPASSWORD="$MSSQL_SA_PASSWORD"

# Provision only the database container. DbUp owns all schema and seed objects.
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -d master \
    -Q "IF DB_ID(N'$database_name') IS NULL EXEC(N'CREATE DATABASE [$database_name]');"
