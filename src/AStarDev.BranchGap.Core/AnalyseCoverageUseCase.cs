using AStarDev.BranchGap.Core.Abstractions;
using AStarDev.BranchGap.Core.Models;

namespace AStarDev.BranchGap.Core;

public sealed class AnalyseCoverageUseCase(ICoverageProvider coverageProvider)
{
    /// <summary>Loads every report, merges them, drops excluded classes and returns the ranked result.</summary>
    public async Task<IReadOnlyList<CoverageClass>> ExecuteAsync(IReadOnlyList<string> paths, IReadOnlyList<string> excludePatterns, CancellationToken cancellationToken)
    {
        var loaded = new List<CoverageClass>();
        foreach (var path in paths)
        {
            loaded.AddRange((await coverageProvider.LoadAsync(path, cancellationToken)).Classes);
        }

        var merged = CoverageMerger.Merge(loaded);

        return CoverageRankingService.Rank(merged.Classes.Where(c => !excludePatterns.Any(p => GlobMatcher.IsMatch(p, c.FullyQualifiedName))));
    }
}
