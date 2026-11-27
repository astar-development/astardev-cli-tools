namespace AStarDev.BranchGap.Core.Models;

public sealed record BranchCoverage(int Covered, int Total)
{
    public int Uncovered => Total - Covered;

    public double Percentage =>
        Total == 0
            ? 100
            : Covered * 100d / Total;
}
