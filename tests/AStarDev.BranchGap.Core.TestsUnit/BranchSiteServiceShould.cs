using AStarDev.BranchGap.Core.Abstractions;
using AStarDev.BranchGap.Core.Models;
using Shouldly;

namespace AStarDev.BranchGap.Core.TestsUnit;

public class BranchSiteServiceShould
{
    private sealed class FakeLocator : IBranchSiteLocator
    {
        public List<string> Files { get; } = [];

        public IReadOnlyList<BranchSite> Locate(string sourceFile, IReadOnlyList<BranchLine> lines)
        {
            Files.Add(sourceFile);
            return [.. lines.Select(l => new BranchSite(sourceFile, l.LineNumber, l.LineNumber < 10 ? "T.A()" : "T.B()", "if", "code", l.Covered, l.Total))];
        }
    }

    private static (string Root, string File) Temp()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        var file = Path.Combine(root, "T.cs");
        File.WriteAllText(file, "x");
        return (root, file);
    }

    [Fact]
    public void rank_members_by_untaken_branches_across_classes_and_skip_fully_covered_ones()
    {
        var (root, _) = Temp();
        var cls = new CoverageClass("A", "T", "/remote/T.cs", new BranchCoverage(3, 10), [new(1, 1, 2, "/remote/T.cs"), new(2, 0, 4, "/remote/T.cs"), new(20, 2, 2, "/remote/T.cs"), new(21, 0, 2, "/remote/T.cs")]);

        var ranked = new BranchSiteService(new FakeLocator()).RankMethods([cls], root);

        ranked.Select(m => (m.Name, m.Coverage.Uncovered)).ShouldBe([("T.A()", 5), ("T.B()", 2)]);
        ranked[0].Sites.Select(s => s.Line).ShouldBe([1, 2]);
        ranked[1].Sites.Select(s => s.Line).ShouldBe([21]);
    }

    [Fact]
    public void skip_classes_whose_source_cannot_be_found()
    {
        var cls = new CoverageClass("A", "T", "/remote/Missing.cs", new BranchCoverage(0, 2), [new(1, 0, 2, "/remote/Missing.cs")]);
        var locator = new FakeLocator();

        new BranchSiteService(locator).SitesFor(cls, Directory.CreateTempSubdirectory().FullName).Count.ShouldBe(0);
        locator.Files.Count.ShouldBe(0);
    }

    [Fact]
    public void ask_the_locator_once_per_source_file_of_a_partial_class()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        File.WriteAllText(Path.Combine(root, "A.cs"), "x");
        File.WriteAllText(Path.Combine(root, "B.cs"), "x");
        var cls = new CoverageClass("A", "T", "A.cs", new BranchCoverage(0, 4), [new(1, 0, 2, "A.cs"), new(2, 0, 2, "B.cs")]);
        var locator = new FakeLocator();

        new BranchSiteService(locator).SitesFor(cls, root).Count.ShouldBe(2);
        locator.Files.Count.ShouldBe(2);
    }
}
