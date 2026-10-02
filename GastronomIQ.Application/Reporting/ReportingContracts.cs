namespace GastronomIQ.Application.Reporting;

public sealed record OperationalDashboardRequest(
    Guid OrganizationId,
    Guid BranchId,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal FoodCost,
    decimal FoodSales,
    decimal PurchasePriceVariance,
    decimal InventoryValue,
    decimal WasteValue,
    decimal ProductionValue,
    decimal PlannedYield,
    decimal ActualYield,
    decimal PlannedProductionCost,
    decimal ActualProductionCost,
    decimal AverageMenuContributionMargin,
    int Stars,
    int Plowhorses,
    int Puzzles,
    int Dogs);

public sealed record OperationalKpiDto(
    string Key,
    string Label,
    decimal Value,
    string Unit,
    string? Currency,
    decimal? Target,
    decimal? Variance);

public sealed record OperationalDashboardDto(
    Guid OrganizationId,
    Guid BranchId,
    DateOnly StartDate,
    DateOnly EndDate,
    IReadOnlyList<OperationalKpiDto> Kpis);

public interface IReportingService
{
    Task<OperationalDashboardDto> BuildDashboardAsync(
        OperationalDashboardRequest request,
        CancellationToken cancellationToken);
}
