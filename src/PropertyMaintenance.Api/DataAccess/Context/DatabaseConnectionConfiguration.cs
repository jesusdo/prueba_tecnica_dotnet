using Microsoft.Data.SqlClient;

namespace PropertyMaintenance.Api.DataAccess.Context;

public static class DatabaseConnectionConfiguration
{
    public static string Validate(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Connection string 'PropertyMaintenance' is required before the API can start.");

        SqlConnectionStringBuilder configuration;
        try
        {
            configuration = new SqlConnectionStringBuilder(connectionString);
        }
        catch (Exception error) when (error is ArgumentException or FormatException or OverflowException)
        {
            // Provider parsing errors can contain connection values. Do not retain them.
            throw new InvalidOperationException(
                "Connection string 'PropertyMaintenance' is invalid. Check the SQL Server connection options.");
        }

        if (!configuration.Enlist)
            throw new InvalidOperationException(
                "Connection string 'PropertyMaintenance' must enable Enlist to preserve transaction guarantees.");

        return connectionString;
    }
}
