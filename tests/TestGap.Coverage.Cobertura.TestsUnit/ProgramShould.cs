using System.Xml.Linq;
using Shouldly;
using TestGap.Core.Abstractions;

namespace TestGap.Coverage.Cobertura.TestsUnit;

public class ProgramShould
{
    private ICoverageProvider provider;
    private ITestOutputHelper output;

    public ProgramShould(ITestOutputHelper output)
    {
        provider = new CoberturaCoverageProvider();
        this.output = output;
    }

    [Fact]
    public async Task loadAsync_and_return_empty_coverage_when_there_are_no_classes()
    {
        var result = await provider.LoadAsync(TestFile.GetPath("EmptyCoverage.xml"), TestContext.Current.CancellationToken);

        result.Classes.Count.ShouldBe(0);
    }

    [Fact]
    public async Task loadAsync_reads_class_and_detects_name_and_namespace_correctly()
    {
        var analysis = await provider.LoadAsync(TestFile.GetPath("RealCoverage.xml"), TestContext.Current.CancellationToken);

        analysis.Classes.Count.ShouldBe(1687);

        analysis.Classes[0].FullyQualifiedName.ShouldBe("AStarDev.CloudSync.Domain.Account");
    }

    [Fact]
    public async Task loadAsync_reads_branch_coverage()
    {
        var analysis = await provider.LoadAsync(TestFile.GetPath("RealCoverage.xml"), TestContext.Current.CancellationToken);

        var sut = analysis.Classes.First(f => f.BranchCoverage.Percentage != 100 && f.BranchCoverage.Percentage != 0);

        sut.BranchCoverage.Covered.ShouldBe(4);
        sut.BranchCoverage.Total.ShouldBe(8);
        sut.BranchCoverage.Percentage.ShouldBe(50);
    }
}