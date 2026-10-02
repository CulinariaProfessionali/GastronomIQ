using GastronomIQ.Application.Reporting;
using GastronomIQ.Domain.Reporting;
using Npgsql;

namespace GastronomIQ.Infrastructure.Reporting;

public sealed class PostgresReportingService : IReportingService
{
    private readonly Persistence.PostgresConnectionFactory _factory;

    public PostgresReportingService(
        Persistence.PostgresConnectionFactory factory) =>
        _factory = factory;

    public async Task<OperationalDashboardDto> BuildDashboardAsync(
        OperationalDashboardRequest request,
        CancellationToken cancellationToken)
    {
        var period = new ReportingPeriod(
            request.StartDate,
            request.EndDate);

        await using var connection = _factory.Create();
        await connection.OpenAsync(cancellationToken);

        await EnsureBranchAccessAsync(
            connection,
            request.OrganizationId,
            request.BranchId,
            cancellationToken);

        // Persisting the computed read-model makes dashboards reproducible
        // for the selected period while the transactional source domains
        // remain authoritative.
        var yieldVariance = ReportingCalculator.VariancePercentage(
            request.ActualYield,
            request.PlannedYield);

        var productionCostVariance = decimal.Round(
            request.ActualProductionCost -
            request.PlannedProductionCost,
            4,
            MidpointRounding.AwayFromZero);

        var foodCostPercentage = ReportingCalculator.FoodCostPercentage(
            request.FoodCost,
            request.FoodSales);

        var wastePercentage = ReportingCalculator.WastePercentage(
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

        var dashboardId = Guid.NewGuid();

        await using var dashboard = new NpgsqlCommand("""
            INSERT INTO reporting_dashboard_snapshots
            (
                id, organization_id, branch_id,
                start_date, end_date
            )
            VALUES
            (
                @id, @organization_id, @branch_id,
                @start_date, @end_date
            );
            """, connection);

        dashboard.Parameters.AddWithValue("id", dashboardId);
        dashboard.Parameters.AddWithValue(
            "organization_id",
            request.OrganizationId);
        dashboard.Parameters.AddWithValue(
            "branch_id",
            request.BranchId);
        dashboard.Parameters.AddWithValue(
            "start_date",
            request.StartDate.ToDateTime(TimeOnly.MinValue));
        dashboard.Parameters.AddWithValue(
            "end_date",
            request.EndDate.ToDateTime(TimeOnly.MaxValue));

        await dashboard.ExecuteNonQueryAsync(cancellationToken);

        foreach (var kpi in kpis)
        {
            await using var insert = new NpgsqlCommand("""
                INSERT INTO reporting_dashboard_kpis
                (
                    id, dashboard_snapshot_id,
                    kpi_key, label, value, unit,
                    currency, target
                )
                VALUES
                (
                    @id, @dashboard,
                    @key, @label, @value, @unit,
                    @currency, @target
                );
                """, connection);

            insert.Parameters.AddWithValue("id", Guid.NewGuid());
            insert.Parameters.AddWithValue("dashboard", dashboardId);
            insert.Parameters.AddWithValue("key", kpi.Key);
            insert.Parameters.AddWithValue("label", kpi.Label);
            insert.Parameters.AddWithValue("value", kpi.Value);
            insert.Parameters.AddWithValue("unit", kpi.Unit);
            insert.Parameters.AddWithValue(
                "currency",
                (object?)kpi.Currency ?? DBNull.Value);
            insert.Parameters.AddWithValue(
                "target",
                (object?)kpi.Target ?? DBNull.Value);

            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        return new OperationalDashboardDto(
            request.OrganizationId,
            request.BranchId,
            period.StartDate,
            period.EndDate,
            kpis.Select(x =>
                new OperationalKpiDto(
                    x.Key,
                    x.Label,
                    x.Value,
                    x.Unit,
                    x.Currency,
                    x.Target,
                    x.Target.HasValue
                        ? decimal.Round(
                            x.Value - x.Target.Value,
                            4)
                        : null))
                .ToArray());
    }

    private static async Task EnsureBranchAccessAsync(
        NpgsqlConnection connection,
        Guid organizationId,
        Guid branchId,
        CancellationToken cancellationToken)
    {
        // The branch table is optional in early installations. If the
        // platform already enforces the tenant/branch context upstream,
        // this remains a no-op; otherwise a branch registry can be wired here.
        await using var command = new NpgsqlCommand("""
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE table_name = 'branches'
                  AND column_name = 'id'
            );
            """, connection);

        var branchTablePresent =
            Convert.ToBoolean(
                await command.ExecuteScalarAsync(cancellationToken));

        if (!branchTablePresent)
            return;

        await using var verify = new NpgsqlCommand("""
            SELECT 1
            FROM branches
            WHERE id = @branch
              AND organization_id = @organization_id;
            """, connection);

        verify.Parameters.AddWithValue("branch", branchId);
        verify.Parameters.AddWithValue(
            "organization_id",
            organizationId);

        if (await verify.ExecuteScalarAsync(cancellationToken) is null)
            throw new UnauthorizedAccessException(
                "Branch does not belong to the requested organization.");
    }
}
