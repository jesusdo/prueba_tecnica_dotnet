using System.Reflection;
using DbUp;
using Microsoft.Extensions.Logging;

namespace PropertyMaintenance.Api.DataAccess.Migrations;

public sealed class DatabaseMigrator
{
    private const string MigrationResourcePrefix =
        "PropertyMaintenance.Api.database.migrations.";

    private readonly ILogger<DatabaseMigrator> logger;

    public DatabaseMigrator(ILogger<DatabaseMigrator> logger)
    {
        this.logger = logger;
    }

    public void Apply(string connectionString)
    {
        logger.LogInformation("Starting database migration check.");

        try
        {
            var upgradeEngine = DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScriptsEmbeddedInAssembly(
                    Assembly.GetExecutingAssembly(),
                    resourceName => resourceName.StartsWith(
                        MigrationResourcePrefix,
                        StringComparison.Ordinal))
                .JournalToSqlTable("dbo", "SchemaVersions")
                .WithTransactionPerScript()
                .Build();

            var pendingScripts = upgradeEngine.GetScriptsToExecute();
            if (pendingScripts.Count == 0)
            {
                logger.LogInformation("Database schema is up to date; no migrations are pending.");
                return;
            }

            logger.LogInformation(
                "Applying {MigrationCount} pending database migrations: {MigrationNames}.",
                pendingScripts.Count,
                string.Join(", ", pendingScripts.Select(script => script.Name)));

            var result = upgradeEngine.PerformUpgrade();
            if (!result.Successful)
            {
                logger.LogError(
                    "Database migration failed; API startup is aborted. ErrorType: {ErrorType}.",
                    result.Error?.GetType().FullName ?? "Unknown");

                throw new InvalidOperationException(
                    "Database migration failed; API startup is aborted.");
            }

            logger.LogInformation(
                "Database migrations completed successfully. Applied {MigrationCount} migration(s).",
                pendingScripts.Count);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                "Database migration could not be completed; API startup is aborted. ErrorType: {ErrorType}.",
                exception.GetType().FullName ?? "Unknown");

            throw new InvalidOperationException(
                "Database migration could not be completed; API startup is aborted.");
        }
    }

    public void ApplyDemoSeed(string connectionString)
    {
        try
        {
            var engine = DeployChanges.To.SqlDatabase(connectionString)
                .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly(),
                    name => name.StartsWith("PropertyMaintenance.Api.database.seed.", StringComparison.Ordinal))
                .JournalToSqlTable("dbo", "DemoSeedVersions")
                .WithTransactionPerScript()
                .Build();

            var pending = engine.GetScriptsToExecute();
            if (pending.Count == 0)
            {
                logger.LogInformation("Demo seed already applied; no changes made.");
                return;
            }

            var result = engine.PerformUpgrade();
            if (!result.Successful)
            {
                throw new InvalidOperationException("Demo seed failed; API startup is aborted.");
            }

            logger.LogInformation("Demo seed completed: {ScriptCount} script(s).", pending.Count);
        }
        catch (Exception)
        {
            // Do not include provider exceptions or connection strings in startup errors.
            logger.LogError("Demo seed failed; API startup is aborted. Inspect the isolated database prerequisites.");
            throw new InvalidOperationException("Demo seed failed; API startup is aborted.");
        }
    }
}
