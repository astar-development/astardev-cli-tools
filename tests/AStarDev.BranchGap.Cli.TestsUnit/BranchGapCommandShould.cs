using Shouldly;
using AStarDev.BranchGap.Cli;

namespace AStarDev.BranchGap.Cli.TestsUnit;

public class BranchGapCommandShould
{
    private static readonly string Report = Path.Combine(AppContext.BaseDirectory, "TestData", "RealCoverage.xml");

    private static async Task<(int Exit, string Out, string Err)> Run(params string[] args)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var exit = await BranchGapCommand.Build(output, error).Parse(args).InvokeAsync(new System.CommandLine.InvocationConfiguration { Output = output, Error = error }, TestContext.Current.CancellationToken);
        return (exit, output.ToString(), error.ToString());
    }

    [Fact]
    public async Task list_the_requested_number_of_worst_classes()
    {
        var (exit, output, _) = await Run(Report, "--top", "3");

        exit.ShouldBe(BranchGapCommand.Success);
        output.ShouldContain("Top 3 classes");
        output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length.ShouldBe(5);
    }

    [Fact]
    public async Task leave_out_classes_matching_an_exclude_glob()
    {
        var (_, output, _) = await Run(Report, "--top", "50", "--exclude", "*.UI.*");

        output.ShouldNotContain(".UI.");
        output.ShouldContain("CloudSync");
    }

    [Fact]
    public async Task merge_the_same_report_given_twice_without_double_counting()
    {
        var single = await Run(Report, "--top", "5");
        var twice = await Run(Report, Report, "--top", "5");

        twice.Out.ShouldBe(single.Out);
    }

    [Fact]
    public async Task fail_with_bad_input_when_a_report_is_missing()
    {
        var (exit, _, error) = await Run("nope.xml");

        exit.ShouldBe(BranchGapCommand.BadInput);
        error.ShouldContain("Report not found: nope.xml");
    }

    [Fact]
    public async Task fail_with_bad_input_when_the_report_is_not_xml()
    {
        var bad = Path.GetTempFileName();
        await File.WriteAllTextAsync(bad, "not xml", TestContext.Current.CancellationToken);

        var (exit, _, error) = await Run(bad);

        exit.ShouldBe(BranchGapCommand.BadInput);
        error.ShouldContain("Not a valid coverage report");
    }

    [Fact]
    public async Task reject_a_top_below_one()
    {
        var (exit, _, error) = await Run(Report, "--top", "0");

        exit.ShouldNotBe(BranchGapCommand.Success);
        error.ShouldContain("--top must be at least 1.");
    }

    private static string Cobertura(string cls, string lines) =>
        $"""<coverage><packages><package name="P"><classes><class name="{cls}" filename="{cls}.cs"><lines>{lines}</lines></class></classes></package></packages></coverage>""";

    private static async Task<string> TempDir(params (string Name, string Xml)[] files)
    {
        var dir = Directory.CreateTempSubdirectory().FullName;
        foreach (var (name, xml) in files)
        {
            await File.WriteAllTextAsync(Path.Combine(dir, name), xml, TestContext.Current.CancellationToken);
        }

        return dir;
    }

    [Fact]
    public async Task merge_reports_so_a_branch_covered_in_any_report_counts_as_covered()
    {
        var dir = await TempDir(
            ("a.cobertura.xml", Cobertura("Foo", """<line number="1" branch="True" condition-coverage="50% (1/2)" /><line number="2" branch="True" condition-coverage="0% (0/2)" />""")),
            ("b.cobertura.xml", Cobertura("Foo", """<line number="1" branch="True" condition-coverage="100% (2/2)" /><line number="2" branch="True" condition-coverage="0% (0/2)" />""")));

        var (exit, output, _) = await Run(Path.Combine(dir, "a.cobertura.xml"), Path.Combine(dir, "b.cobertura.xml"));

        exit.ShouldBe(BranchGapCommand.Success);
        output.ShouldContain("2 untaken");
        output.ShouldContain("(2/4)");
    }

    [Fact]
    public async Task expand_a_wildcard_report_argument()
    {
        var dir = await TempDir(
            ("a.cobertura.xml", Cobertura("Foo", """<line number="1" branch="True" condition-coverage="0% (0/2)" />""")),
            ("b.cobertura.xml", Cobertura("Bar", """<line number="1" branch="True" condition-coverage="0% (0/4)" />""")),
            ("ignore.txt", "x"));

        var (_, output, _) = await Run(Path.Combine(dir, "*.cobertura.xml"));

        output.ShouldContain("Foo");
        output.ShouldContain("Bar");
    }

    [Fact]
    public async Task fail_with_bad_input_when_a_wildcard_matches_nothing()
    {
        var dir = await TempDir();

        var (exit, _, error) = await Run(Path.Combine(dir, "*.xml"));

        exit.ShouldBe(BranchGapCommand.BadInput);
        error.ShouldContain("Report not found");
    }

    [Fact]
    public async Task search_subfolders_when_the_directory_contains_a_double_star()
    {
        var dir = await TempDir();
        Directory.CreateDirectory(Path.Combine(dir, "x", "y"));
        await File.WriteAllTextAsync(Path.Combine(dir, "x", "y", "deep.cobertura.xml"), Cobertura("Deep", """<line number="1" branch="True" condition-coverage="0% (0/2)" />"""), TestContext.Current.CancellationToken);

        var (_, output, _) = await Run(Path.Combine(dir, "**", "*.cobertura.xml"));

        output.ShouldContain("Deep");
    }

    private static async Task<string> HalfCovered() =>
        Path.Combine(await TempDir(("r.cobertura.xml", Cobertura("Foo", """<line number="1" branch="True" condition-coverage="50% (1/2)" />"""))), "r.cobertura.xml");

    [Fact]
    public async Task show_the_overall_branch_coverage()
    {
        var (_, output, _) = await Run(await HalfCovered());

        output.ShouldContain("Overall branch coverage: 50.0% (1/2)");
    }

    [Fact]
    public async Task write_json_when_asked()
    {
        var (exit, output, _) = await Run(await HalfCovered(), "--format", "json");

        exit.ShouldBe(BranchGapCommand.Success);
        var json = System.Text.Json.JsonDocument.Parse(output).RootElement;
        json.GetProperty("overall").GetProperty("total").GetInt32().ShouldBe(2);
        json.GetProperty("classes")[0].GetProperty("name").GetString().ShouldBe("Foo");
        json.GetProperty("classes")[0].GetProperty("untaken").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task write_a_markdown_table_when_asked()
    {
        var (_, output, _) = await Run(await HalfCovered(), "--format", "markdown");

        output.ShouldContain("| Class | Untaken | Covered | Total | % |");
        output.ShouldContain("| `Foo` | 1 | 1 | 2 | 50 |");
    }

    [Fact]
    public async Task exit_with_one_when_overall_coverage_is_below_fail_under()
    {
        var (exit, _, error) = await Run(await HalfCovered(), "--fail-under", "60");

        exit.ShouldBe(BranchGapCommand.BelowThreshold);
        error.ShouldContain("Branch coverage 50.0% is below the required 60%.");
    }

    [Fact]
    public async Task exit_with_zero_when_overall_coverage_meets_fail_under()
    {
        var (exit, _, _) = await Run(await HalfCovered(), "--fail-under", "50");

        exit.ShouldBe(BranchGapCommand.Success);
    }

    [Fact]
    public async Task reject_a_fail_under_outside_zero_to_one_hundred()
    {
        var (exit, _, error) = await Run(await HalfCovered(), "--fail-under", "101");

        exit.ShouldNotBe(BranchGapCommand.Success);
        error.ShouldContain("--fail-under must be between 0 and 100.");
    }
}
