namespace AStarDev.BranchGap.Core.Models;

/// <summary>A branch line resolved to its source: the member it lives in and the construct that branches.</summary>
public sealed record BranchSite(string SourceFile, int Line, string Member, string Construct, string Code, int Covered, int Total)
{
    public int Uncovered => Total - Covered;
}

public sealed record MethodGap(string Name, string SourceFile, BranchCoverage Coverage, IReadOnlyList<BranchSite> Sites);
