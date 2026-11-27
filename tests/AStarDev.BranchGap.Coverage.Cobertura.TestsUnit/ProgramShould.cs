using System.Xml.Linq;
using Shouldly;
using AStarDev.BranchGap.Core;
using AStarDev.BranchGap.Core.Abstractions;
using AStarDev.BranchGap.Core.Models;

namespace AStarDev.BranchGap.Coverage.Cobertura.TestsUnit;

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

        analysis.Classes.Count.ShouldBe(703);

        analysis.Classes[0].FullyQualifiedName.ShouldBe("AStarDev.CloudSync.Classification.ClassificationCandidate");
        analysis.Classes[0].AssemblyName.ShouldBe("AStarDev.CloudSync.Classification");
    }

    [Fact]
    public async Task loadAsync_reads_branch_coverage()
    {
        var analysis = await provider.LoadAsync(TestFile.GetPath("RealCoverage.xml"), TestContext.Current.CancellationToken);

        var sut = analysis.Classes.First(f => f.BranchCoverage.Percentage != 100 && f.BranchCoverage.Percentage != 0);

        sut.BranchCoverage.Covered.ShouldBe(1);
        sut.BranchCoverage.Total.ShouldBe(2);
        sut.BranchCoverage.Percentage.ShouldBe(50);
    }

    [Fact]
    public void loadAsync_ranks_by_lowest_branch_coverage()
    {
        List<CoverageClass> classes =
        [
            new CoverageClass(
            AssemblyName: "Test",
            FullyQualifiedName: "A",
            SourceFile: "A.cs",
            BranchCoverage: new BranchCoverage(90, 100)),

        new CoverageClass(
            AssemblyName: "Test",
            FullyQualifiedName: "B",
            SourceFile: "B.cs",
            BranchCoverage: new BranchCoverage(20, 100)),

        new CoverageClass(
            AssemblyName: "Test",
            FullyQualifiedName: "C",
            SourceFile: "C.cs",
            BranchCoverage: new BranchCoverage(50, 100))
        ];

        var result = CoverageRankingService.Rank(classes);

        result[0].FullyQualifiedName.ShouldBe("B");
        result[1].FullyQualifiedName.ShouldBe("C");
        result[2].FullyQualifiedName.ShouldBe("A");
    }

    [Fact]
    public async Task loadAsync_reads_branch_coverage_from_multiple_classes()
    {
        var analysis = await provider.LoadAsync(TestFile.GetPath("RealCoverage.xml"), TestContext.Current.CancellationToken);

        var sut = analysis.Classes.Where(f => f.BranchCoverage.Percentage != 100 && f.BranchCoverage.Percentage != 0);

        sut.Sum(s=>s.BranchCoverage.Covered).ShouldBe(2139);
        sut.Sum(s=>s.BranchCoverage.Total).ShouldBe(2633);
        sut.Average(s=>s.BranchCoverage.Percentage).ShouldBe(77.4153397786392, 1e-9);
    }

    [Fact]
    public async Task roll_nested_and_generated_types_into_the_declaring_type_and_drop_module()
    {
        var analysis = await provider.LoadAsync(TestFile.GetPath("NestedClasses.xml"), TestContext.Current.CancellationToken);

        analysis.Classes.Count.ShouldBe(1);
        var foo = analysis.Classes[0];
        foo.FullyQualifiedName.ShouldBe("My.Ns.Foo");
        foo.AssemblyName.ShouldBe("My.Assembly");
        foo.BranchCoverage.ShouldBe(new BranchCoverage(3, 8));
    }

    [Fact]
    public async Task count_a_branch_line_once_when_the_class_repeats_it_under_a_method()
    {
        var analysis = await provider.LoadAsync(TestFile.GetPath("NestedClasses.xml"), TestContext.Current.CancellationToken);

        analysis.Classes[0].BranchLines.Single(l => l.LineNumber == 10).ShouldBe(new BranchLine(10, 1, 2, "Foo.cs"));
    }
}
