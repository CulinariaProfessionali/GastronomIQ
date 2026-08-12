namespace GastronomIQ.Domain.Reporting;

public sealed record ReportingPeriod
{
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }

    public ReportingPeriod(
        DateOnly startDate,
        DateOnly endDate)
    {
        if (endDate < startDate)
            throw new ArgumentException("End date cannot precede start date.");

        StartDate = startDate;
        EndDate = endDate;
    }
}

public sealed record OperationalKpi(
    string Key,
    string Label,
    decimal Value,
    string Unit,
    string? Currency = null,
    decimal? Target = null)
{
    public decimal? Variance =>
        Target.HasValue
            ? decimal.Round(Value - Target.Value, 4)
            : null;
}

public sealed record OperationalDashboard(
    Guid OrganizationId,
    Guid BranchId,
    ReportingPeriod Period,
    IReadOnlyList<OperationalKpi> Kpis);

public static class ReportingCalculator
{
    public static decimal FoodCostPercentage(
        decimal foodCost,
        decimal sales)
    {
        if (sales <= 0) return 0;

        return decimal.Round(
            foodCost / sales * 100m,
            2,
            MidpointRounding.AwayFromZero);
    }

    public static decimal VariancePercentage(
        decimal actual,
        decimal planned)
    {
        if (planned == 0) return 0;

        return decimal.Round(
            (actual - planned) / planned * 100m,
            2,
            MidpointRounding.AwayFromZero);
    }

    public static decimal WastePercentage(
        decimal wasteValue,
        decimal productionValue)
    {
        if (productionValue <= 0) return 0;

        return decimal.Round(
            wasteValue / productionValue * 100m,
            2,
            MidpointRounding.AwayFromZero);
    }
}
