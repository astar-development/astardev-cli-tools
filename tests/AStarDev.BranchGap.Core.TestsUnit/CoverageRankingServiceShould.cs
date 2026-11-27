using AStarDev.BranchGap.Core.Models;
using Shouldly;

namespace AStarDev.BranchGap.Core.TestsUnit;

public class CoverageRankingServiceShould
{
    private static CoverageClass Make(string name, int covered, int total) =>
        new("Asm", name, $"{name}.cs", new BranchCoverage(covered, total));

    [Fact]
    public void rank_by_most_untaken_branches_not_lowest_percentage()
    {
        var result = CoverageRankingService.Rank([Make("Tiny", 0, 2), Make("Big", 200, 400)]);

        result.Select(r => r.FullyQualifiedName).ShouldBe(["Big", "Tiny"]);
    }

    [Fact]
    public void break_ties_by_lowest_percentage_then_name()
    {
        var result = CoverageRankingService.Rank([Make("B", 5, 10), Make("A", 5, 10), Make("C", 0, 5)]);

        result.Select(r => r.FullyQualifiedName).ShouldBe(["C", "A", "B"]);
    }

    [Fact]
    public void drop_classes_without_branches_and_compiler_generated_classes()
    {
        var result = CoverageRankingService.Rank([Make("NoBranches", 0, 0), Make("Foo.<Bar>d__1", 0, 4), Make("Real", 1, 4)]);

        result.Select(r => r.FullyQualifiedName).ShouldBe(["Real"]);
    }

    [Fact]
    public void keep_real_classes_whose_name_merely_contains_generated()
    {
        var result = CoverageRankingService.Rank([Make("App.GeneratedInvoice", 0, 4)]);

        result.Select(r => r.FullyQualifiedName).ShouldBe(["App.GeneratedInvoice"]);
    }
}
