namespace TestGap.Core.Models;

public sealed record BranchCoverage(int Covered, int Total)
{
    public double Percentage =>
        Total == 0
            ? 100
            : Covered * 100d / Total;
}