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

public static class ReportFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string Format(OutputFormat format, IReadOnlyList<CoverageClass> rows, BranchCoverage overall) => format switch
    {
        OutputFormat.Json => Json(rows, overall),
        OutputFormat.Markdown => Markdown(rows, overall),
        _ => Table(rows, overall),
    };

    private static string Summary(BranchCoverage overall) =>
        $"Overall branch coverage: {overall.Percentage:0.0}% ({overall.Covered}/{overall.Total})";

    private static string Table(IReadOnlyList<CoverageClass> rows, BranchCoverage overall)
    {
        var text = new StringBuilder();
        text.AppendLine(Summary(overall));
        text.AppendLine($"Top {rows.Count} classes by most untaken branches:");
        foreach (var row in rows)
        {
            text.AppendLine($"{row.BranchCoverage.Uncovered,5} untaken  {row.BranchCoverage.Percentage,5:0.#}%  ({row.BranchCoverage.Covered}/{row.BranchCoverage.Total})  {row.FullyQualifiedName}");
        }

        return text.ToString();
    }

    private static string Markdown(IReadOnlyList<CoverageClass> rows, BranchCoverage overall)
    {
        var text = new StringBuilder();
        text.AppendLine($"**{Summary(overall)}**");
        text.AppendLine();
        text.AppendLine("| Class | Untaken | Covered | Total | % |");
        text.AppendLine("|---|---:|---:|---:|---:|");
        foreach (var row in rows)
        {
            text.AppendLine($"| `{row.FullyQualifiedName}` | {row.BranchCoverage.Uncovered} | {row.BranchCoverage.Covered} | {row.BranchCoverage.Total} | {row.BranchCoverage.Percentage:0.#} |");
        }

        return text.ToString();
    }

    private static string Json(IReadOnlyList<CoverageClass> rows, BranchCoverage overall) =>
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
                }),
            },
            JsonOptions) + Environment.NewLine;
}
