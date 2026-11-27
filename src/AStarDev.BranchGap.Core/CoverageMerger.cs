using AStarDev.BranchGap.Core.Models;

namespace AStarDev.BranchGap.Core;

public static class CoverageMerger
{
    /// <summary>
    /// Combines entries that share an assembly and type name (several reports, or a type and its nested types).
    /// Per branch line the entry with the best covered count wins (its total comes with it).
    /// </summary>
    // shortcut: Cobertura only gives "n of m" per line, not which branch, so the max of the inputs is used - it may under-report the union; upgrade if a format with per-branch ids is added.
    public static CoverageAnalysis Merge(IEnumerable<CoverageClass> classes) =>
        new([.. classes
            .GroupBy(c => (c.AssemblyName, c.FullyQualifiedName))
            .Select(MergeGroup)
            .OrderBy(c => c.FullyQualifiedName, StringComparer.Ordinal)]);

    private static CoverageClass MergeGroup(IGrouping<(string AssemblyName, string FullyQualifiedName), CoverageClass> group)
    {
        var members = group.ToList();
        var lines = members
            .SelectMany(m => m.BranchLines)
            .GroupBy(l => (File: l.SourceFile.Replace('\\', '/'), l.LineNumber))
            .Select(g => g.OrderByDescending(l => l.Covered).ThenByDescending(l => l.Total).First())
            .OrderBy(l => l.SourceFile, StringComparer.Ordinal).ThenBy(l => l.LineNumber)
            .ToList();

        var coverage = lines.Count > 0
            ? new BranchCoverage(lines.Sum(l => l.Covered), lines.Sum(l => l.Total))
            : new BranchCoverage(members.Sum(m => m.BranchCoverage.Covered), members.Sum(m => m.BranchCoverage.Total));

        return new CoverageClass(group.Key.AssemblyName, group.Key.FullyQualifiedName, members[0].SourceFile, coverage, lines);
    }
}
