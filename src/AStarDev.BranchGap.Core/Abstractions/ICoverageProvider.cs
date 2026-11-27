using AStarDev.BranchGap.Core.Models;

namespace AStarDev.BranchGap.Core.Abstractions;

public interface ICoverageProvider
{
    Task<CoverageAnalysis> LoadAsync(string path, CancellationToken cancellationToken);
}
