using System.Text.RegularExpressions;
using System.Xml.Linq;
using TestGap.Core.Abstractions;
using TestGap.Core.Models;

namespace TestGap.Coverage.Cobertura;

public sealed class CoberturaCoverageProvider : ICoverageProvider
{
    public Task<CoverageAnalysis> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        var document = XDocument.Load(path);
        var classes = document
                        .Descendants("class")
                        .Select(x => new CoverageClass(
                            AssemblyName: string.Empty,
                            FullyQualifiedName: x.Attribute("name")?.Value ?? string.Empty,
                            SourceFile: x.Attribute("filename")?.Value ?? string.Empty,
                            BranchCoverage: GetBranchCoverage(x)))
                        .ToList();

        return Task.FromResult(new CoverageAnalysis(classes));
    }
    private static BranchCoverage GetBranchCoverage(XElement classElement)
{
    var branchLines = classElement
        .Descendants("line")
        .Where(x =>
            string.Equals(
                x.Attribute("branch")?.Value,
                "True",
                StringComparison.OrdinalIgnoreCase));

    var covered = 0;
    var total = 0;

    foreach (var line in branchLines)
    {
        var conditionCoverage =
            line.Attribute("condition-coverage")?.Value;

        if (string.IsNullOrWhiteSpace(conditionCoverage))
        {
            continue;
        }

        var match = Regex.Match(
            conditionCoverage,
            @"\((\d+)\/(\d+)\)");

        if (!match.Success)
        {
            continue;
        }

        covered += int.Parse(match.Groups[1].Value);
        total += int.Parse(match.Groups[2].Value);
    }

    return new BranchCoverage(
        Covered: covered,
        Total: total);
}
}
