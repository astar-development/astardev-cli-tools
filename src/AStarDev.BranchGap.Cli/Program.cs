using AStarDev.BranchGap.Core;
using AStarDev.BranchGap.Coverage.Cobertura;

var path = args.Length > 0 ? args[0] : string.Empty;
var itemsToTake = args.Length > 1 ? int.Parse(args[1]) : 10;
var coberturaCoverageProvider = new CoberturaCoverageProvider();
var classes = await coberturaCoverageProvider.LoadAsync(path, CancellationToken.None);
var worstRankings = CoverageRankingService.Rank(classes.Classes).Take(itemsToTake).ToList();

Console.WriteLine($"Top {itemsToTake} Classes by Most Untaken Branches:");
Console.WriteLine("--------------------------------------------------------------------------------------------" + Environment.NewLine);

foreach (var ranking in worstRankings)
{
    Console.WriteLine($"Class: {ranking.FullyQualifiedName}, Branch Coverage: {ranking.BranchCoverage.Percentage:0.#}% (Covered: {ranking.BranchCoverage.Covered} / Total: {ranking.BranchCoverage.Total})");
}

// dotnet publish -c Release -r linux-x64 -o ./publish; clear; ./publish/AStarDev.BranchGap.Cli tests/AStarDev.BranchGap.Coverage.Cobertura.TestsUnit/TestData/RealCoverage.xml