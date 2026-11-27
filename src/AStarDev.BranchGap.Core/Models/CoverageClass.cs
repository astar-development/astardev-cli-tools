namespace AStarDev.BranchGap.Core.Models;

public sealed record CoverageClass(
    string AssemblyName,
    string FullyQualifiedName,
    string SourceFile,
    BranchCoverage BranchCoverage,
    IReadOnlyList<BranchLine> BranchLines)
{
    public CoverageClass(string AssemblyName, string FullyQualifiedName, string SourceFile, BranchCoverage BranchCoverage)
        : this(AssemblyName, FullyQualifiedName, SourceFile, BranchCoverage, [])
    {
    }

    public bool IsCompilerGenerated =>
        FullyQualifiedName.Contains('<') || FullyQualifiedName.Contains("Generated", StringComparison.Ordinal);
}
