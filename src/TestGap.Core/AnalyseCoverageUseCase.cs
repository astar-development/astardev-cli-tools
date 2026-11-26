using TestGap.Core.Abstractions;
using TestGap.Core.Models;

namespace TestGap.Core;

public sealed class AnalyseCoverageUseCase(ICoverageProvider coverageProvider)
{
    private readonly ICoverageProvider coverageProvider = coverageProvider;

    public async Task<CoverageAnalysis> ExecuteAsync(string path, CancellationToken cancellationToken) => await coverageProvider.LoadAsync(path, cancellationToken);
}
