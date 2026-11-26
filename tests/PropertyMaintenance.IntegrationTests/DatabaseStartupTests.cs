using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PropertyMaintenance.Api.DataAccess.Migrations;

namespace PropertyMaintenance.IntegrationTests;

public sealed class DatabaseStartupTests
{
    [Fact]
    public async Task DisabledEnlistmentStopsBeforeMigrationResolutionOrServerStart()
    {
        var migrationResolved = false;
        var lifetime = new StartupObserver();
        await using var baseFactory = new RequestFactory(
            "Server=db,1433;Database=PropertyMaintenanceDb_T04_T102_20261126;Enlist=false",
            DateTimeOffset.UtcNow);
        await using var factory = baseFactory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<DatabaseMigrator>();
            services.AddSingleton<DatabaseMigrator>(_ =>
            {
                migrationResolved = true;
                throw new InvalidOperationException("Migration resolution must not be reached.");
            });
            services.AddSingleton<IHostedService>(lifetime);
        }));

        var error = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        Assert.False(migrationResolved);
        Assert.False(lifetime.Started);
        Assert.Contains("Enlist", error.Message);
        Assert.Null(error.InnerException);
    }

    private sealed class StartupObserver : IHostedService
    {
        public bool Started { get; private set; }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Started = true;
            return Task.CompletedTask;
        }
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
