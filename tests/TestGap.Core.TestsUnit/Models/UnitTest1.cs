using Shouldly;
using TestGap.Core.Models;

namespace TestGap.Core.TestsUnit.Models;

public class CoverageClassShould
{
    [Fact]
    public void calculate_the_branch_coverage_percentage()
    {
        var coverage = new CoverageClass(
            FullyQualifiedName: "Foo.Bar.SyncEngine",
            SourceFile: "SyncEngine.cs",
            BranchCoverage: new BranchCoverage(Covered: 5, Total: 10));

        coverage.BranchCoverage.Percentage.ShouldBe(50);
    }
}
