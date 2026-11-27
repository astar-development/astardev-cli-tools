namespace AStarDev.BranchGap.Core.Models;

public sealed record BranchLine(int LineNumber, int Covered, int Total)
{
    public int Uncovered => Total - Covered;
}
