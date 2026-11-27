using System.CommandLine;
using System.Xml;
using AStarDev.BranchGap.Core;
using AStarDev.BranchGap.Core.Abstractions;
using AStarDev.BranchGap.Core.Models;
using AStarDev.BranchGap.Coverage.Cobertura;
using AStarDev.BranchGap.Roslyn;

namespace AStarDev.BranchGap.Cli;

public static class BranchGapCommand
{
    public const int Success = 0;
    public const int BelowThreshold = 1;
    public const int BadInput = 2;

    public static RootCommand Build(TextWriter output, TextWriter error, ICoverageProvider? provider = null, IBranchSiteLocator? locator = null)
    {
        var reports = new Argument<string[]>("reports") { Description = "Cobertura XML report(s); several are merged.", Arity = ArgumentArity.OneOrMore };
        var top = new Option<int>("--top", "-n") { Description = "How many classes to list.", DefaultValueFactory = _ => 10 };
        top.Validators.Add(r => { if (r.GetValueOrDefault<int>() < 1) { r.AddError("--top must be at least 1."); } });
        var exclude = new Option<string[]>("--exclude", "-x") { Description = "Class-name glob (* and ?) to leave out. Repeatable.", DefaultValueFactory = _ => [] };

        var format = new Option<OutputFormat>("--format", "-f") { Description = "Output format: table, json or markdown.", DefaultValueFactory = _ => OutputFormat.Table };
        var failUnder = new Option<double?>("--fail-under") { Description = "Exit with code 1 when overall branch coverage is below this percentage (0-100)." };
        failUnder.Validators.Add(r => { if (r.GetValueOrDefault<double?>() is < 0 or > 100) { r.AddError("--fail-under must be between 0 and 100."); } });

        var sourceRoot = new Option<DirectoryInfo?>("--source-root") { Description = "Folder holding the sources named in the report. Shows which construct each untaken branch is (needs the sources, not just the report)." };
        var level = new Option<ReportLevel>("--level") { Description = "Rank classes (default) or members. Members need --source-root.", DefaultValueFactory = _ => ReportLevel.Class };

        var root = new RootCommand("Lists the classes with the most untaken branches.") { reports, top, exclude, format, failUnder, sourceRoot, level };
        root.SetAction(async (parse, cancellationToken) =>
        {
            var requested = parse.GetValue(reports)!;
            var unresolved = requested.FirstOrDefault(r => ReportPaths.Expand([r]) is not { Count: > 0 } found || found.Any(f => !File.Exists(f)));
            if (unresolved is not null)
            {
                await error.WriteLineAsync($"Report not found: {unresolved}");
                return BadInput;
            }

            var paths = ReportPaths.Expand(requested);
            var source = parse.GetValue(sourceRoot);
            if (source is { Exists: false })
            {
                await error.WriteLineAsync($"Source root not found: {source.FullName}");
                return BadInput;
            }

            if (parse.GetValue(level) == ReportLevel.Method && source is null)
            {
                await error.WriteLineAsync("--level method needs --source-root.");
                return BadInput;
            }

            try
            {
                var ranked = await new AnalyseCoverageUseCase(provider ?? new CoberturaCoverageProvider())
                    .ExecuteAsync(paths, parse.GetValue(exclude)!, cancellationToken);
                var siteService = new BranchSiteService(locator ?? new RoslynBranchSiteLocator());
                if (parse.GetValue(level) == ReportLevel.Method)
                {
                    var members = siteService.RankMethods(ranked.Classes, source!.FullName);
                    await output.WriteAsync(ReportFormatter.FormatMethods(parse.GetValue(format), [.. members.Take(parse.GetValue(top))], ranked.Overall));
                }
                else
                {
                    var shown = ranked.Classes.Take(parse.GetValue(top)).ToList();
                    var sites = source is null
                        ? null
                        : shown.GroupBy(c => c.FullyQualifiedName).ToDictionary(g => g.Key, g => (IReadOnlyList<BranchSite>)[.. g.SelectMany(c => siteService.SitesFor(c, source.FullName)).Where(s => s.Uncovered > 0).OrderBy(s => s.Line)]);
                    await output.WriteAsync(ReportFormatter.Format(parse.GetValue(format), shown, ranked.Overall, sites));
                }

                var threshold = parse.GetValue(failUnder);
                if (threshold is not null && ranked.Overall.Percentage < threshold)
                {
                    await error.WriteLineAsync($"Branch coverage {ranked.Overall.Percentage:0.0}% is below the required {threshold}%.");
                    return BelowThreshold;
                }

                return Success;
            }
            catch (XmlException ex)
            {
                await error.WriteLineAsync($"Not a valid coverage report: {ex.Message}");
                return BadInput;
            }
        });

        return root;
    }
}
