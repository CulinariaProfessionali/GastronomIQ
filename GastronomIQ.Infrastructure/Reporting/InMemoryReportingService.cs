using GastronomIQ.Application.Reporting;
using GastronomIQ.Domain.Reporting;

namespace GastronomIQ.Infrastructure.Reporting;

public sealed class InMemoryReportingService : IReportingService
{
    public Task<OperationalDashboardDto> BuildDashboardAsync(
        OperationalDashboardRequest request,
        CancellationToken cancellationToken)
    {
        var period = new ReportingPeriod(
            request.StartDate,
            request.EndDate);

        var yieldVariance = ReportingCalculator.VariancePercentage(
            request.ActualYield,
            request.PlannedYield);

        var productionCostVariance =
            decimal.Round(
                request.ActualProductionCost -
                request.PlannedProductionCost,
                4,
                MidpointRounding.AwayFromZero);

        var foodCostPercentage =
            ReportingCalculator.FoodCostPercentage(
                request.FoodCost,
                request.FoodSales);

        var wastePercentage =
            ReportingCalculator.WastePercentage(
                request.WasteValue,
                request.ProductionValue);

        var kpis = new[]
        {
            new OperationalKpi(
                "FOOD_COST_PERCENT",
                "Food Cost %",
                foodCostPercentage,
                "%"),

            new OperationalKpi(
                "PURCHASE_PRICE_VARIANCE",
                "Purchase Price Variance",
                request.PurchasePriceVariance,
                "CURRENCY",
                "INR"),

            new OperationalKpi(
                "INVENTORY_VALUE",
                "Inventory Value",
                request.InventoryValue,
                "CURRENCY",
                "INR"),

            new OperationalKpi(
                "WASTE_VALUE",
                "Waste Value",
                request.WasteValue,
                "CURRENCY",
                "INR"),

            new OperationalKpi(
                "WASTE_PERCENT",
                "Waste %",
                wastePercentage,
                "%"),

            new OperationalKpi(
                "YIELD_VARIANCE_PERCENT",
                "Yield Variance %",
                yieldVariance,
                "%"),

            new OperationalKpi(
                "PRODUCTION_COST_VARIANCE",
                "Production Cost Variance",
                productionCostVariance,
                "CURRENCY",
                "INR"),

            new OperationalKpi(
                "AVERAGE_MENU_CONTRIBUTION",
                "Average Menu Contribution Margin",
                request.AverageMenuContributionMargin,
                "CURRENCY",
                "INR"),

            new OperationalKpi(
                "MENU_STARS",
                "Menu Stars",
                request.Stars,
                "COUNT"),

            new OperationalKpi(
                "MENU_PLOWHORSES",
                "Menu Plowhorses",
                request.Plowhorses,
                "COUNT"),

            new OperationalKpi(
                "MENU_PUZZLES",
                "Menu Puzzles",
                request.Puzzles,
                "COUNT"),

            new OperationalKpi(
                "MENU_DOGS",
                "Menu Dogs",
                request.Dogs,
                "COUNT")
        };

        return Task.FromResult(
            new OperationalDashboardDto(
                request.OrganizationId,
                request.BranchId,
                period.StartDate,
                period.EndDate,
                kpis.Select(Map).ToArray()));
    }

    private static OperationalKpiDto Map(OperationalKpi kpi) =>
        new(
            kpi.Key,
            kpi.Label,
            kpi.Value,
            kpi.Unit,
            kpi.Currency,
            kpi.Target,
            kpi.Variance);
}
