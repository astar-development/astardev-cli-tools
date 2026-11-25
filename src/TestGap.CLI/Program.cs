using TestGap.Core;
using TestGap.Coverage.Cobertura;

var path = args.Length > 0 ? args[0] : string.Empty;
var itemsToTake = args.Length > 1 ? int.Parse(args[1]) : 10;
var coberturaCoverageProvider = new CoberturaCoverageProvider();
var classes = await coberturaCoverageProvider.LoadAsync(path, CancellationToken.None);
var rankings = CoverageRankingService.Rank(classes.Classes);

Console.WriteLine($"Top {itemsToTake} Classes by Lowest Branch Coverage:");
System.Console.WriteLine("--------------------------------------------------------------------------------------------" + Environment.NewLine);
var worstRankings = rankings.Where(r => ClassesToIgnore(r)).OrderBy(r => r.BranchCoverage.Percentage).ThenByDescending(r => r.BranchCoverage.Total).Take(itemsToTake).ToList();

foreach (var ranking in worstRankings)
{
    Console.WriteLine($"Class: {ranking.FullyQualifiedName}, Branch Coverage: {ranking.BranchCoverage.Percentage}% ({ranking.BranchCoverage.Covered}/{ranking.BranchCoverage.Total})");
}

static bool ClassesToIgnore(TestGap.Core.Models.CoverageClass r)
{
    return !r.FullyQualifiedName.Contains("Generated") && !r.FullyQualifiedName.Contains('<');
}

// dotnet publish -c Release -r linux-x64 -o ./publish; clear; ./publish/TestGap.CLI tests/TestGap.Coverage.Cobertura.TestsUnit/TestData/RealCoverage.xml