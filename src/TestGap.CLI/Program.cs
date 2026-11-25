using TestGap.Core;
using TestGap.Coverage.Cobertura;

var path = args.Length > 0 ? args[0] : string.Empty;
var coberturaCoverageProvider = new CoberturaCoverageProvider();
var classes = await coberturaCoverageProvider.LoadAsync(path, CancellationToken.None);
var rankings = CoverageRankingService.Rank(classes.Classes);

rankings.ToList().ForEach(Console.WriteLine);

// dotnet publish -c Release -r linux-x64 -o ./publish 