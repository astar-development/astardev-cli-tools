using System.CommandLine;
using System.Xml;
using AStarDev.BranchGap.Core;
using AStarDev.BranchGap.Core.Abstractions;
using AStarDev.BranchGap.Core.Models;
using AStarDev.BranchGap.Coverage.Cobertura;

namespace AStarDev.BranchGap.Cli;

public static class BranchGapCommand
{
    public const int Success = 0;
    public const int BadInput = 2;

    public static RootCommand Build(TextWriter output, TextWriter error, ICoverageProvider? provider = null)
    {
        var reports = new Argument<string[]>("reports") { Description = "Cobertura XML report(s); several are merged.", Arity = ArgumentArity.OneOrMore };
        var top = new Option<int>("--top", "-n") { Description = "How many classes to list.", DefaultValueFactory = _ => 10 };
        top.Validators.Add(r => { if (r.GetValueOrDefault<int>() < 1) { r.AddError("--top must be at least 1."); } });
        var exclude = new Option<string[]>("--exclude", "-x") { Description = "Class-name glob (* and ?) to leave out. Repeatable.", DefaultValueFactory = _ => [] };

        var root = new RootCommand("Lists the classes with the most untaken branches.") { reports, top, exclude };
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

            try
            {
                var ranked = await new AnalyseCoverageUseCase(provider ?? new CoberturaCoverageProvider())
                    .ExecuteAsync(paths, parse.GetValue(exclude)!, cancellationToken);
                await Render(output, ranked.Take(parse.GetValue(top)).ToList());
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

    private static async Task Render(TextWriter output, IReadOnlyList<CoverageClass> rows)
    {
        await output.WriteLineAsync($"Top {rows.Count} classes by most untaken branches:");
        foreach (var row in rows)
        {
            await output.WriteLineAsync($"{row.BranchCoverage.Uncovered,5} untaken  {row.BranchCoverage.Percentage,5:0.#}%  ({row.BranchCoverage.Covered}/{row.BranchCoverage.Total})  {row.FullyQualifiedName}");
        }
    }
}
