namespace TestGap.Core.Models;

public sealed record CoverageClass(string FullyQualifiedName, string SourceFile, BranchCoverage BranchCoverage);
