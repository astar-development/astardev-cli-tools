using System.Text;
using System.Text.Json;
using AStarDev.BranchGap.Core.Models;

namespace AStarDev.BranchGap.Cli;

public enum OutputFormat
{
    Table,
    Json,
    Markdown,
}

public enum ReportLevel
{
    Class,
    Method,
}

public static class ReportFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<BranchSite>> NoSites = new Dictionary<string, IReadOnlyList<BranchSite>>();

    /// <param name="sites">Untaken branch sites per class name; empty when no source root was given.</param>
    public static string Format(OutputFormat format, IReadOnlyList<CoverageClass> rows, BranchCoverage overall, IReadOnlyDictionary<string, IReadOnlyList<BranchSite>>? sites = null)
    {
        sites ??= NoSites;
        return format switch
        {
            OutputFormat.Json => Json(rows, overall, sites),
            OutputFormat.Markdown => Markdown(rows, overall, sites),
            _ => Table(rows, overall, sites),
        };
    }

    public static string FormatMethods(OutputFormat format, IReadOnlyList<MethodGap> rows, BranchCoverage overall) => format switch
    {
        OutputFormat.Json => MethodsJson(rows, overall),
        OutputFormat.Markdown => MethodsMarkdown(rows, overall),
        _ => MethodsTable(rows, overall),
    };

    private static string Summary(BranchCoverage overall) =>
        $"Overall branch coverage: {overall.Percentage:0.0}% ({overall.Covered}/{overall.Total})";

    private static string SiteLine(BranchSite s) =>
        $"line {s.Line}: {s.Construct}, {s.Uncovered} of {s.Total} untaken: {s.Code}";

    private static string MarkdownSiteLine(BranchSite s) =>
        $"line {s.Line}: {s.Construct}, {s.Uncovered} of {s.Total} untaken: {CodeSpan(s.Code)}";

    // A fence one backtick longer than any run inside, padded so a leading or trailing backtick survives.
    private static string CodeSpan(string code)
    {
        var longest = 0;
        var run = 0;
        foreach (var c in code)
        {
            run = c == '`' ? run + 1 : 0;
            longest = Math.Max(longest, run);
        }

        var fence = new string('`', longest + 1);
        return $"{fence} {code} {fence}";
    }

    private static string Cell(string text) => text.Replace("|", "\\|", StringComparison.Ordinal);

    private static string Table(IReadOnlyList<CoverageClass> rows, BranchCoverage overall, IReadOnlyDictionary<string, IReadOnlyList<BranchSite>> sites)
    {
        var text = new StringBuilder();
        text.AppendLine(Summary(overall));
        text.AppendLine($"Top {rows.Count} classes by most untaken branches:");
        foreach (var row in rows)
        {
            text.AppendLine($"{row.BranchCoverage.Uncovered,5} untaken  {row.BranchCoverage.Percentage,5:0.#}%  ({row.BranchCoverage.Covered}/{row.BranchCoverage.Total})  {row.FullyQualifiedName}");
            foreach (var site in sites.GetValueOrDefault(row.FullyQualifiedName) ?? [])
            {
                text.AppendLine($"                 {site.Member} {SiteLine(site)}");
            }
        }

        return text.ToString();
    }

    private static string Markdown(IReadOnlyList<CoverageClass> rows, BranchCoverage overall, IReadOnlyDictionary<string, IReadOnlyList<BranchSite>> sites)
    {
        var text = new StringBuilder();
        text.AppendLine($"**{Summary(overall)}**");
        text.AppendLine();
        text.AppendLine("| Class | Untaken | Covered | Total | % |");
        text.AppendLine("|---|---:|---:|---:|---:|");
        foreach (var row in rows)
        {
            text.AppendLine($"| `{Cell(row.FullyQualifiedName)}` | {row.BranchCoverage.Uncovered} | {row.BranchCoverage.Covered} | {row.BranchCoverage.Total} | {row.BranchCoverage.Percentage:0.#} |");
        }

        foreach (var row in rows.Where(r => sites.GetValueOrDefault(r.FullyQualifiedName) is { Count: > 0 }))
        {
            text.AppendLine();
            text.AppendLine($"`{row.FullyQualifiedName}`");
            foreach (var site in sites[row.FullyQualifiedName])
            {
                text.AppendLine($"- {CodeSpan(site.Member)} {MarkdownSiteLine(site)}");
            }
        }

        return text.ToString();
    }

    private static object ToJson(BranchSite s) => new { member = s.Member, line = s.Line, construct = s.Construct, untaken = s.Uncovered, total = s.Total, code = s.Code };

    private static string Json(IReadOnlyList<CoverageClass> rows, BranchCoverage overall, IReadOnlyDictionary<string, IReadOnlyList<BranchSite>> sites) =>
        JsonSerializer.Serialize(
            new
            {
                overall = new { covered = overall.Covered, total = overall.Total, percentage = Math.Round(overall.Percentage, 1) },
                classes = rows.Select(r => new
                {
                    name = r.FullyQualifiedName,
                    assembly = r.AssemblyName,
                    sourceFile = r.SourceFile,
                    covered = r.BranchCoverage.Covered,
                    total = r.BranchCoverage.Total,
                    untaken = r.BranchCoverage.Uncovered,
                    percentage = Math.Round(r.BranchCoverage.Percentage, 1),
                    sites = (sites.GetValueOrDefault(r.FullyQualifiedName) ?? []).Select(ToJson),
                }),
            },
            JsonOptions) + Environment.NewLine;

    private static string MethodsTable(IReadOnlyList<MethodGap> rows, BranchCoverage overall)
    {
        var text = new StringBuilder();
        text.AppendLine(Summary(overall));
        text.AppendLine($"Top {rows.Count} members by most untaken branches:");
        foreach (var row in rows)
        {
            text.AppendLine($"{row.Coverage.Uncovered,5} untaken  {row.Coverage.Percentage,5:0.#}%  ({row.Coverage.Covered}/{row.Coverage.Total})  {row.Name}  [{Path.GetFileName(row.SourceFile)}]");
            foreach (var site in row.Sites)
            {
                text.AppendLine($"                 {SiteLine(site)}");
            }
        }

        return text.ToString();
    }

    private static string MethodsMarkdown(IReadOnlyList<MethodGap> rows, BranchCoverage overall)
    {
        var text = new StringBuilder();
        text.AppendLine($"**{Summary(overall)}**");
        text.AppendLine();
        text.AppendLine("| Member | File | Untaken | Covered | Total | % |");
        text.AppendLine("|---|---|---:|---:|---:|---:|");
        foreach (var row in rows)
        {
            text.AppendLine($"| `{Cell(row.Name)}` | {Cell(Path.GetFileName(row.SourceFile))} | {row.Coverage.Uncovered} | {row.Coverage.Covered} | {row.Coverage.Total} | {row.Coverage.Percentage:0.#} |");
        }

        return text.ToString();
    }

    private static string MethodsJson(IReadOnlyList<MethodGap> rows, BranchCoverage overall) =>
        JsonSerializer.Serialize(
            new
            {
                overall = new { covered = overall.Covered, total = overall.Total, percentage = Math.Round(overall.Percentage, 1) },
                members = rows.Select(r => new
                {
                    name = r.Name,
                    sourceFile = r.SourceFile,
                    covered = r.Coverage.Covered,
                    total = r.Coverage.Total,
                    untaken = r.Coverage.Uncovered,
                    percentage = Math.Round(r.Coverage.Percentage, 1),
                    sites = r.Sites.Select(ToJson),
                }),
            },
            JsonOptions) + Environment.NewLine;
}
