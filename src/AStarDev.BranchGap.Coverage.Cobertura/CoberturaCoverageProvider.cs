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
        var lines = GetBranchLines(classElement);
        return new CoverageClass(
            AssemblyName: classElement.Parent?.Parent?.Attribute("name")?.Value ?? string.Empty,
            FullyQualifiedName: ParentTypeName(classElement.Attribute("name")?.Value ?? string.Empty),
            SourceFile: classElement.Attribute("filename")?.Value ?? string.Empty,
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

    private static List<BranchLine> GetBranchLines(XElement classElement) =>
        [.. classElement
            .Descendants("line")
            .Where(x => string.Equals(x.Attribute("branch")?.Value, "True", StringComparison.OrdinalIgnoreCase))
            .Select(ToBranchLine)
            .OfType<BranchLine>()
            .GroupBy(l => l.LineNumber)
            .Select(g => g.MaxBy(l => l.Covered)!)];

    private static BranchLine? ToBranchLine(XElement line)
    {
        var match = ConditionCoverage().Match(line.Attribute("condition-coverage")?.Value ?? string.Empty);

        return match.Success && int.TryParse(line.Attribute("number")?.Value, out var number)
            ? new BranchLine(number, int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value))
            : null;
    }

    [GeneratedRegex(@"\((\d+)/(\d+)\)")]
    private static partial Regex ConditionCoverage();
}
