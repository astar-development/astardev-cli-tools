namespace AStarDev.BranchGap.Core.Models;

/// <summary>Branch coverage for one source line. <paramref name="SourceFile"/> is per line because partial and nested types can span files.</summary>
public sealed record BranchLine(int LineNumber, int Covered, int Total, string SourceFile = "")
{
    public int Uncovered => Total - Covered;
}
