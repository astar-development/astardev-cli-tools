using AStarDev.BranchGap.Core.Models;

namespace AStarDev.BranchGap.Core;

public static class CoverageRankingService
{
    /// <summary>Largest number of untaken branches first; lowest percentage breaks ties. Classes without branches are dropped.</summary>
    public static IReadOnlyList<CoverageClass> Rank(IEnumerable<CoverageClass> classes) =>
        [.. classes
            .Where(c => c.BranchCoverage.Total > 0 && !c.IsExcludedFromReporting)
            .OrderByDescending(c => c.BranchCoverage.Uncovered)
            .ThenBy(c => c.BranchCoverage.Percentage)
            .ThenBy(c => c.FullyQualifiedName, StringComparer.Ordinal)];
}
