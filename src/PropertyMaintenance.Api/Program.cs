using PropertyMaintenance.Api.DataAccess.Migrations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSingleton<DatabaseMigrator>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

var connectionString = app.Configuration.GetConnectionString("PropertyMaintenance");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'PropertyMaintenance' is required before the API can start.");
}

using (var scope = app.Services.CreateScope())
{
    var databaseMigrator = scope.ServiceProvider.GetRequiredService<DatabaseMigrator>();
    databaseMigrator.Apply(connectionString);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
