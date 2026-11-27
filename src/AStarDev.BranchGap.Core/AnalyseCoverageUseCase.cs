using AStarDev.BranchGap.Core.Abstractions;
using AStarDev.BranchGap.Core.Models;

namespace AStarDev.BranchGap.Core;

public sealed class AnalyseCoverageUseCase(ICoverageProvider coverageProvider)
{
    private readonly ICoverageProvider coverageProvider = coverageProvider;

    public async Task<CoverageAnalysis> ExecuteAsync(string path, CancellationToken cancellationToken) => await coverageProvider.LoadAsync(path, cancellationToken);
}
