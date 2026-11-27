using AStarDev.BranchGap.Core.Models;

namespace AStarDev.BranchGap.Core;

public sealed class CoverageRankingService
{
    public static IReadOnlyList<CoverageClass> Rank(IEnumerable<CoverageClass> classes) =>
        [.. classes.OrderBy(x => x.BranchCoverage.Percentage)];
}
