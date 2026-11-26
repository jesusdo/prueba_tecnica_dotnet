using Microsoft.Data.SqlClient;

namespace PropertyMaintenance.IntegrationTests;

[Collection("SQL")]
public sealed class SchemaValidationTests(SqlFixture fixture)
{
    internal static string ReadScript()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "database", "validation", "ValidateSchema.sql")))
            root = root.Parent;
        return File.ReadAllText(Path.Combine(root?.FullName ?? throw new InvalidOperationException("Repository validation script not found."),
            "database", "validation", "ValidateSchema.sql"));
    }

    [Fact]
    public async Task FullyInitializedDatabaseWithBothJournalsIsValid()
    {
        await using var connection = new SqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = ReadScript();
        var result = await command.ExecuteScalarAsync();
        Assert.StartsWith("PASS:", Assert.IsType<string>(result));
    }

    [Theory]
    [InlineData("EXEC sys.sp_rename N'dbo.Properties',N'T102MissingProperty';", 51000)]
    [InlineData("CREATE TABLE dbo.T102UnexpectedTable(Id int NOT NULL PRIMARY KEY);", 51001)]
    public async Task MissingRequiredAndUnexpectedTablesStillFail(string mutation, int expectedError)
    {
        await using var connection = new SqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = mutation;
            await command.ExecuteNonQueryAsync();
            command.CommandText = ReadScript();
            var error = await Assert.ThrowsAsync<SqlException>(() => command.ExecuteNonQueryAsync());
            Assert.Equal(expectedError, error.Number);
        }
        finally
        {
            // THROW with XACT_ABORT can doom the transaction; rollback restores all DDL.
            await transaction.RollbackAsync();
        }
    }
}
