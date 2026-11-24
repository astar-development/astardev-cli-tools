using System.Xml.Linq;
using TestGap.Core.Abstractions;
using TestGap.Core.Models;

namespace TestGap.Coverage.Cobertura;

public sealed class CoberturaCoverageProvider : ICoverageProvider
{
    public Task<CoverageAnalysis> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        var document = XDocument.Load(path);
        var classes =
    document
        .Descendants("class")
        .Select(x => new CoverageClass(
            AssemblyName: string.Empty,
            FullyQualifiedName: x.Attribute("name")?.Value ?? string.Empty,
            SourceFile: x.Attribute("filename")?.Value ?? string.Empty,
            BranchCoverage: new BranchCoverage(0, 0)))
        .ToList();

        return Task.FromResult(
    new CoverageAnalysis(classes));
    }
}
