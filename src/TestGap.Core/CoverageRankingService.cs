using TestGap.Core.Models;

namespace TestGap.Core;

public sealed class CoverageRankingService
{
    public static IReadOnlyList<CoverageClass> Rank(IEnumerable<CoverageClass> classes) =>
        [.. classes.OrderBy(x => x.BranchCoverage.Percentage)];
}
