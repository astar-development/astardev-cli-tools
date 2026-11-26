using TestGap.Core.Models;

namespace TestGap.Core.Abstractions;

public interface ICoverageProvider
{
    Task<CoverageAnalysis> LoadAsync(string path, CancellationToken cancellationToken);
}
