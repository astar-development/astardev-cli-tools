using AStarDev.BranchGap.Core.Models;
using Shouldly;

namespace AStarDev.BranchGap.Core.TestsUnit;

public class CoverageMergerShould
{
    private static CoverageClass Make(string name, params BranchLine[] lines) =>
        new("Asm", name, $"{name}.cs", new BranchCoverage(lines.Sum(l => l.Covered), lines.Sum(l => l.Total)), lines);

    [Fact]
    public void take_the_best_covered_count_for_a_line_seen_in_several_reports()
    {
        var merged = CoverageMerger.Merge([Make("Foo", new BranchLine(5, 1, 2)), Make("Foo", new BranchLine(5, 2, 2))]);

        merged.Classes.Count.ShouldBe(1);
        merged.Classes[0].BranchCoverage.ShouldBe(new BranchCoverage(2, 2));
    }

    [Fact]
    public void keep_lines_that_only_one_report_covers()
    {
        var merged = CoverageMerger.Merge([Make("Foo", new BranchLine(5, 1, 2)), Make("Foo", new BranchLine(9, 0, 2))]);

        merged.Classes[0].BranchCoverage.ShouldBe(new BranchCoverage(1, 4));
        merged.Classes[0].BranchLines.Select(l => l.LineNumber).ShouldBe([5, 9]);
    }

    [Fact]
    public void keep_distinct_classes_apart()
    {
        var merged = CoverageMerger.Merge([Make("B", new BranchLine(1, 0, 2)), Make("A", new BranchLine(1, 2, 2))]);

        merged.Classes.Select(c => c.FullyQualifiedName).ShouldBe(["A", "B"]);
    }

    [Fact]
    public void sum_totals_when_entries_carry_no_line_detail()
    {
        var merged = CoverageMerger.Merge([new CoverageClass("Asm", "Foo", "Foo.cs", new BranchCoverage(1, 2)), new CoverageClass("Asm", "Foo", "Foo.cs", new BranchCoverage(2, 4))]);

        merged.Classes[0].BranchCoverage.ShouldBe(new BranchCoverage(3, 6));
    }
}
