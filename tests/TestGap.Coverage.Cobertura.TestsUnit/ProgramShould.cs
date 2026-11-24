using Shouldly;
using TestGap.Core.Abstractions;

namespace TestGap.Coverage.Cobertura.TestsUnit;

public class ProgramShould
{
    private ICoverageProvider provider;

    public ProgramShould() => provider = new CoberturaCoverageProvider();

    [Fact]
    public async Task loadAsync_and_return_empty_coverage_when_there_are_no_classes()
    {
        var provider = new CoberturaCoverageProvider();

        var result = await provider.LoadAsync("TestData/EmptyCoverage.xml", TestContext.Current.CancellationToken);

        result.Classes.Count.ShouldBe(0);
    }
}
