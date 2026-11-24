using TestGap.Core.Abstractions;
using TestGap.Core.Models;

namespace TestGap.Coverage.Cobertura;

public sealed class CoberturaCoverageProvider : ICoverageProvider
{
    public Task<CoverageAnalysis> LoadAsync(string path, CancellationToken cancellationToken = default) =>
        Task.FromResult(new CoverageAnalysis([]));
}
