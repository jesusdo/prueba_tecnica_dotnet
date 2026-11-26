using PropertyMaintenance.Api.DataAccess.Context;

namespace PropertyMaintenance.UnitTests;

public sealed class DatabaseConnectionConfigurationTests
{
    [Theory]
    [InlineData("Server=db,1433;Database=PropertyMaintenanceDb_T04_T102_20261126")]
    [InlineData("Server=db,1433;Database=PropertyMaintenanceDb_T04_T102_20261126;Enlist=true")]
    public void DefaultAndExplicitEnlistmentPreserveConnectionString(string connection)
    {
        Assert.Equal(connection, DatabaseConnectionConfiguration.Validate(connection));
    }

    [Theory]
    [InlineData(null, "required")]
    [InlineData("", "required")]
    [InlineData("   ", "required")]
    [InlineData("Server=db,1433;Enlist=false", "Enlist")]
    [InlineData("Server=db,1433;Enlist=invalid", "invalid")]
    [InlineData("UnsupportedOption=fixture-sensitive-value", "invalid")]
    [InlineData("Server=\"unterminated", "invalid")]
    [InlineData("Connect Timeout=999999999999999999999999", "invalid")]
    public void InvalidConfigurationFailsWithSafeError(string? connection, string expected)
    {
        var error = Assert.Throws<InvalidOperationException>(() => DatabaseConnectionConfiguration.Validate(connection));

        Assert.Contains(expected, error.Message);
        Assert.Null(error.InnerException);
        Assert.DoesNotContain("fixture-sensitive-value", error.ToString());
        Assert.DoesNotContain("UnsupportedOption", error.ToString());
        Assert.DoesNotContain("unterminated", error.ToString());
    }

    [Theory]
    [InlineData("Password=fixture-sensitive-value;Enlist=false")]
    [InlineData("Password=fixture-sensitive-value;Enlist=invalid")]
    public void RejectedConnectionNeverIncludesCredentialValuesOrProviderException(string connection)
    {
        var error = Assert.Throws<InvalidOperationException>(() => DatabaseConnectionConfiguration.Validate(connection));

        Assert.Null(error.InnerException);
        Assert.DoesNotContain("fixture-sensitive-value", error.ToString());
        Assert.DoesNotContain("Password", error.ToString());
    }
}
