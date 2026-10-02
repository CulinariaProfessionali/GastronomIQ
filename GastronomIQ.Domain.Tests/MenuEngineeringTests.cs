using GastronomIQ.Domain.MenuEngineering;

namespace GastronomIQ.Domain.Tests;

public class MenuEngineeringTests
{
    [Fact]
    public void Classify_assigns_deterministic_classes()
    {
        var input = new[]
        {
            new MenuPerformanceInput(Guid.NewGuid(), "A", 200, 80, 100),
            new MenuPerformanceInput(Guid.NewGuid(), "B", 180, 140, 120),
            new MenuPerformanceInput(Guid.NewGuid(), "C", 250, 90, 40),
            new MenuPerformanceInput(Guid.NewGuid(), "D", 120, 100, 35)
        };

        var result = MenuEngineeringCalculator.Classify(input);

        Assert.Equal(4, result.Count);
        Assert.Equal(MenuEngineeringClass.Star, result.Single(x => x.Name == "A").Classification);
        Assert.Equal(MenuEngineeringClass.Plowhorse, result.Single(x => x.Name == "B").Classification);
        Assert.Equal(MenuEngineeringClass.Puzzle, result.Single(x => x.Name == "C").Classification);
        Assert.Equal(MenuEngineeringClass.Dog, result.Single(x => x.Name == "D").Classification);
    }
}
