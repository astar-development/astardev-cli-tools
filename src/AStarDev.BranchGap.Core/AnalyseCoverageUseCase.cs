using AStarDev.BranchGap.Core.Abstractions;
using AStarDev.BranchGap.Core.Models;

namespace AStarDev.BranchGap.Core;

public sealed class AnalyseCoverageUseCase(ICoverageProvider coverageProvider)
{
    /// <summary>Loads every report, merges them, drops excluded classes and returns the ranked classes plus the overall branch figure for what remains.</summary>
    public async Task<RankedCoverage> ExecuteAsync(IReadOnlyList<string> paths, IReadOnlyList<string> excludePatterns, CancellationToken cancellationToken)
    {
        var loaded = new List<CoverageClass>();
        foreach (var path in paths)
        {
            loaded.AddRange((await coverageProvider.LoadAsync(path, cancellationToken)).Classes);
        }

        var merged = CoverageMerger.Merge(loaded);

        var ranked = CoverageRankingService.Rank(merged.Classes.Where(c => !excludePatterns.Any(p => GlobMatcher.IsMatch(p, c.FullyQualifiedName))));

        return new RankedCoverage(ranked, new BranchCoverage(ranked.Sum(c => c.BranchCoverage.Covered), ranked.Sum(c => c.BranchCoverage.Total)));
    }
}
