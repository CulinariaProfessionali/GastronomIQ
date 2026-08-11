using GastronomIQ.Domain.Reporting;

namespace GastronomIQ.Domain.Tests;

public class ReportingTests
{
    [Fact]
    public void Food_cost_percentage_is_calculated()
    {
        Assert.Equal(
            30,
            ReportingCalculator.FoodCostPercentage(300, 1000));
    }

    [Fact]
    public void Yield_variance_percentage_is_calculated()
    {
        Assert.Equal(
            -5,
            ReportingCalculator.VariancePercentage(95, 100));
    }

    [Fact]
    public void Waste_percentage_is_calculated()
    {
        Assert.Equal(
            2,
            ReportingCalculator.WastePercentage(20, 1000));
    }

    [Fact]
    public void Invalid_reporting_period_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new ReportingPeriod(
                new DateOnly(2026, 8, 10),
                new DateOnly(2026, 8, 9)));
    }
}
