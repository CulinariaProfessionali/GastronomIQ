using GastronomIQ.Api.Configuration;
using GastronomIQ.Api.Endpoints;
using GastronomIQ.Api.Middleware;
using GastronomIQ.Application.Authorization;
using GastronomIQ.Application.Billing;
using GastronomIQ.Application.Costing;
using GastronomIQ.Application.Identity;
using GastronomIQ.Application.Inventory;
using GastronomIQ.Application.MenuEngineering;
using GastronomIQ.Application.Platform;
using GastronomIQ.Application.Procurement;
using GastronomIQ.Application.Production;
using GastronomIQ.Application.Recipes;
using GastronomIQ.Application.Reporting;
using GastronomIQ.Application.Settings;
using GastronomIQ.Infrastructure.Authorization;
using GastronomIQ.Infrastructure.Billing;
using GastronomIQ.Infrastructure.Costing;
using GastronomIQ.Infrastructure.Identity;
using GastronomIQ.Infrastructure.Inventory;
using GastronomIQ.Infrastructure.MenuEngineering;
using GastronomIQ.Infrastructure.Platform;
using GastronomIQ.Infrastructure.Persistence;
using GastronomIQ.Infrastructure.Procurement;
using GastronomIQ.Infrastructure.Production;
using GastronomIQ.Infrastructure.Recipes;
using GastronomIQ.Infrastructure.Reporting;
using GastronomIQ.Infrastructure.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services.AddSingleton<IAuthenticationService, InMemoryAuthenticationService>();
builder.Services.AddSingleton<IAuthorizationService, AuthorizationService>();
builder.Services.AddSingleton<IEntitlementService, InMemoryEntitlementService>();
builder.Services.AddSingleton<IOrganizationSettingsService, InMemoryOrganizationSettingsService>();
builder.Services.AddSingleton<IRecipeService, InMemoryRecipeService>();
builder.Services.AddSingleton<IRecipeCostingService, RecipeCostingService>();
var configuredPostgres = builder.Configuration.GetConnectionString("GastronomIQ");
var usePostgres = !string.IsNullOrWhiteSpace(configuredPostgres);

builder.Services.AddSingleton<IInventoryService, InMemoryInventoryService>();
builder.Services.AddSingleton<IProcurementService, InMemoryProcurementService>();
builder.Services.AddSingleton<IProductionService, InMemoryProductionService>();
builder.Services.AddSingleton<IMenuEngineeringService, InMemoryMenuEngineeringService>();
builder.Services.AddSingleton<IReportingService, InMemoryReportingService>();

if (usePostgres)
{
    builder.Services.AddSingleton<PostgresConnectionFactory>(
        _ => new PostgresConnectionFactory(configuredPostgres!));
    builder.Services.AddSingleton<IInventoryService, PostgresInventoryService>();
    builder.Services.AddSingleton<IProcurementService, PostgresProcurementService>();
    builder.Services.AddSingleton<IProductionService, PostgresProductionService>();
    builder.Services.AddSingleton<IMenuEngineeringService, PostgresMenuEngineeringService>();
    builder.Services.AddSingleton<IReportingService, PostgresReportingService>();
}

builder.Services.AddSingleton<ITenantContextAccessor, InMemoryTenantContextAccessor>();
builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
builder.Services.AddSingleton<IAuditWriter, InMemoryAuditWriter>();
builder.Services.AddSingleton<IUnitOfWork, InMemoryUnitOfWork>();

if (usePostgres)
{
    builder.Services.AddSingleton<IIdempotencyStore, PostgresIdempotencyStore>();
    builder.Services.AddSingleton<IAuditWriter, PostgresAuditWriter>();
    builder.Services.AddSingleton<ITransactionManager, PostgresTransactionManager>();
    builder.Services.AddSingleton<IMigrationRunner>(sp =>
        new PostgresMigrationRunner(
            sp.GetRequiredService<PostgresConnectionFactory>(),
            Path.Combine(builder.Environment.ContentRootPath, "database", "schema")));
}

var configuration = new ProductionConfiguration {
    Environment = builder.Configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development",
    DatabaseConnectionString = builder.Configuration.GetConnectionString("GastronomIQ") ?? string.Empty
};

builder.Services.AddSingleton(configuration);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseMiddleware<IdempotencyMiddleware>();

if (configuration.RequireHttps)
    app.UseHttpsRedirection();

app.MapHealthChecks("/health");
app.MapReadinessEndpoints();
app.MapAuthEndpoints();
app.MapSettingsEndpoints();
app.MapRecipeEndpoints();
app.MapCostingEndpoints();
app.MapInventoryEndpoints();
app.MapProcurementEndpoints();
app.MapProductionEndpoints();
app.MapMenuEngineeringEndpoints();
app.MapReportingEndpoints();

app.Run();

public partial class Program { }
