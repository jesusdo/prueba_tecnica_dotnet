using PropertyMaintenance.Api.DataAccess.Migrations;
using Microsoft.EntityFrameworkCore;
using PropertyMaintenance.Api.DataAccess.Context;
using PropertyMaintenance.Api.DataAccess.Repositories;
using PropertyMaintenance.Api.Business.Services;
using PropertyMaintenance.Api.Exceptions;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<MaintenanceRequestService>();
builder.Services.AddScoped<VisitService>();
builder.Services.AddScoped<IVisitRepository, VisitRepository>();
builder.Services.AddScoped<IUnitRepository, UnitRepository>();
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<ITechnicianRepository, TechnicianRepository>();
builder.Services.AddSingleton<DatabaseMigrator>();
builder.Services.AddDbContext<PropertyMaintenanceDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PropertyMaintenance")));
builder.Services.AddScoped<ILeaseRepository, LeaseRepository>();
builder.Services.AddScoped<IMaintenanceRequestRepository, MaintenanceRequestRepository>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_1;
    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        // Route integers are not JSON bodies. Describe their parsed type without
        // the integer/string union produced by the web JSON number handling.
        foreach (var parameter in operation.Parameters ?? [])
            if (parameter.In == Microsoft.OpenApi.ParameterLocation.Path
                && parameter.Schema is Microsoft.OpenApi.OpenApiSchema { Format: "int32" } schema)
                schema.Type = Microsoft.OpenApi.JsonSchemaType.Integer;
        return Task.CompletedTask;
    });
});

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
    if (app.Configuration.GetValue<bool>("Database:SeedDemoData"))
    {
        databaseMigrator.ApplyDemoSeed(connectionString);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Property Maintenance v1"));
}

app.UseExceptionHandler();

app.MapControllers();

app.Run();

public partial class Program { }
