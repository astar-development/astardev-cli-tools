using AStarDev.BranchGap.Core.Abstractions;
using AStarDev.BranchGap.Core.Models;

namespace AStarDev.BranchGap.Core;

public sealed class BranchSiteService(IBranchSiteLocator locator)
{
    /// <summary>Every branch site of the class whose source can be found under <paramref name="sourceRoot"/>; files that cannot be found are skipped.</summary>
    public IReadOnlyList<BranchSite> SitesFor(CoverageClass coverageClass, string sourceRoot)
    {
        var sites = new List<BranchSite>();
        foreach (var file in coverageClass.BranchLines.GroupBy(l => l.SourceFile))
        {
            if (SourceFileResolver.Resolve(file.Key, sourceRoot) is { } resolved)
            {
                sites.AddRange(locator.Locate(resolved, [.. file]));
            }
        }

        return sites;
    }

    /// <summary>Ranks members, across all the given classes, by untaken branches.</summary>
    public IReadOnlyList<MethodGap> RankMethods(IEnumerable<CoverageClass> classes, string sourceRoot) =>
        [.. classes
            .SelectMany(c => SitesFor(c, sourceRoot))
            .GroupBy(s => (s.SourceFile, s.Member))
            .Select(g => new MethodGap(g.Key.Member, g.Key.SourceFile, new BranchCoverage(g.Sum(s => s.Covered), g.Sum(s => s.Total)), [.. g.Where(s => s.Uncovered > 0).OrderBy(s => s.Line)]))
            .Where(m => m.Coverage.Uncovered > 0)
            .OrderByDescending(m => m.Coverage.Uncovered)
            .ThenBy(m => m.Coverage.Percentage)
            .ThenBy(m => m.Name, StringComparer.Ordinal)];
}
