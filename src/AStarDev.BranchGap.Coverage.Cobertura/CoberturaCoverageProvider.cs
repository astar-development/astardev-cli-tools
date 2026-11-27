using System.Text.RegularExpressions;
using System.Xml.Linq;
using AStarDev.BranchGap.Core;
using AStarDev.BranchGap.Core.Abstractions;
using AStarDev.BranchGap.Core.Models;

namespace AStarDev.BranchGap.Coverage.Cobertura;

public sealed partial class CoberturaCoverageProvider : ICoverageProvider
{
    public Task<CoverageAnalysis> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        var document = XDocument.Load(path);
        var classes = document
            .Descendants("class")
            .Select(ToCoverageClass)
            .Where(c => c.FullyQualifiedName.Length > 0);

        return Task.FromResult(CoverageMerger.Merge(classes));
    }

    private static CoverageClass ToCoverageClass(XElement classElement)
    {
        var sourceFile = classElement.Attribute("filename")?.Value ?? string.Empty;
        var lines = GetBranchLines(classElement, sourceFile);
        return new CoverageClass(
            AssemblyName: classElement.Parent?.Parent?.Attribute("name")?.Value ?? string.Empty,
            FullyQualifiedName: ParentTypeName(classElement.Attribute("name")?.Value ?? string.Empty),
            SourceFile: sourceFile,
            BranchCoverage: new BranchCoverage(lines.Sum(l => l.Covered), lines.Sum(l => l.Total)),
            BranchLines: lines);
    }

    /// <summary>Rolls nested and compiler-generated types (<c>Foo/Bar</c>, <c>Foo.&lt;&gt;c</c>, <c>Foo.&lt;M&gt;d__1</c>) up into the declaring type; names with no declaring type (<c>&lt;Module&gt;</c>) become empty.</summary>
    internal static string ParentTypeName(string name)
    {
        var slash = name.IndexOf('/', StringComparison.Ordinal);
        if (slash >= 0)
        {
            return name[..slash];
        }

        var generated = name.IndexOf(".<", StringComparison.Ordinal);
        if (generated >= 0)
        {
            return name[..generated];
        }

        return name.StartsWith('<') ? string.Empty : name;
    }

    private static List<BranchLine> GetBranchLines(XElement classElement, string sourceFile) =>
        [.. classElement
            .Descendants("line")
            .Where(x => string.Equals(x.Attribute("branch")?.Value, "True", StringComparison.OrdinalIgnoreCase))
            .Select(x => ToBranchLine(x, sourceFile))
            .OfType<BranchLine>()
            .GroupBy(l => l.LineNumber)
            .Select(g => g.MaxBy(l => l.Covered)!)];

    private static BranchLine? ToBranchLine(XElement line, string sourceFile)
    {
        var match = ConditionCoverage().Match(line.Attribute("condition-coverage")?.Value ?? string.Empty);

        return match.Success
            && int.TryParse(line.Attribute("number")?.Value, out var number)
            && int.TryParse(match.Groups[1].ValueSpan, out var covered)
            && int.TryParse(match.Groups[2].ValueSpan, out var total)
            && covered <= total
                ? new BranchLine(number, covered, total, sourceFile)
                : null;
    }

    [GeneratedRegex(@"\(([0-9]+)/([0-9]+)\)")]
    private static partial Regex ConditionCoverage();
}
