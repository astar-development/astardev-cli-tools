namespace AStarDev.BranchGap.Core.Models;

public sealed record CoverageClass(
    string AssemblyName,
    string FullyQualifiedName,
    string SourceFile,
    BranchCoverage BranchCoverage,
    IReadOnlyList<BranchLine> BranchLines)
{
    private static readonly string[] ThirdPartyNamespaceRoots =
    [
        "Amazon",
        "AutoMapper",
        "Azure",
        "Dapper",
        "FluentAssertions",
        "FluentValidation",
        "Google",
        "Hangfire",
        "MediatR",
        "Microsoft",
        "Moq",
        "Newtonsoft",
        "Npgsql",
        "Polly",
        "Quartz",
        "RabbitMQ",
        "Refit",
        "RestSharp",
        "Serilog",
        "StackExchange",
        "System",
        "Xunit",
        "xunit"
    ];

    public CoverageClass(string AssemblyName, string FullyQualifiedName, string SourceFile, BranchCoverage BranchCoverage)
        : this(AssemblyName, FullyQualifiedName, SourceFile, BranchCoverage, [])
    {
    }

    public bool IsCompilerGenerated => FullyQualifiedName.Contains('<');
    public bool IsExcludedFromReporting =>
        IsCompilerGenerated
        || ThirdPartyNamespaceRoots.Any(IsNamespace);

    private bool IsNamespace(string namespaceName) =>
        FullyQualifiedName.Equals(namespaceName, StringComparison.Ordinal)
        || FullyQualifiedName.StartsWith(namespaceName + ".", StringComparison.Ordinal);
}
