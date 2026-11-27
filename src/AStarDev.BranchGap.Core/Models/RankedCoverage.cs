namespace AStarDev.BranchGap.Core.Models;

public sealed record RankedCoverage(IReadOnlyList<CoverageClass> Classes, BranchCoverage Overall);
