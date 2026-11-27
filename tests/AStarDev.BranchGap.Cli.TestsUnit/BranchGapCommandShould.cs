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

    private static async Task<(string Report, string Root)> ReportWithSource()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        await File.WriteAllTextAsync(Path.Combine(root, "Foo.cs"), "namespace N;\npublic class Foo\n{\n    public int Bar(int a)\n    {\n        if (a > 0)\n        {\n            return 1;\n        }\n\n        return 2;\n    }\n}\n", TestContext.Current.CancellationToken);
        var report = Path.Combine(root, "r.xml");
        await File.WriteAllTextAsync(report, """<coverage><packages><package name="P"><classes><class name="N.Foo" filename="/build/agent/src/Foo.cs"><lines><line number="6" branch="True" condition-coverage="50% (1/2)" /></lines></class></classes></package></packages></coverage>""", TestContext.Current.CancellationToken);
        return (report, root);
    }

    [Fact]
    public async Task show_the_untaken_construct_per_class_when_given_a_source_root()
    {
        var (report, root) = await ReportWithSource();

        var (exit, output, _) = await Run(report, "--source-root", root);

        exit.ShouldBe(BranchGapCommand.Success);
        output.ShouldContain("Foo.Bar(int) line 6: if, 1 of 2 untaken: if (a > 0)");
    }

    [Fact]
    public async Task rank_members_when_level_is_method()
    {
        var (report, root) = await ReportWithSource();

        var (exit, output, _) = await Run(report, "--source-root", root, "--level", "method");

        exit.ShouldBe(BranchGapCommand.Success);
        output.ShouldContain("1 untaken");
        output.ShouldContain("Foo.Bar(int)");
        output.ShouldContain("line 6: if");
    }

    [Fact]
    public async Task carry_the_sites_in_json()
    {
        var (report, root) = await ReportWithSource();

        var (_, output, _) = await Run(report, "--source-root", root, "--format", "json");

        var site = System.Text.Json.JsonDocument.Parse(output).RootElement.GetProperty("classes")[0].GetProperty("sites")[0];
        site.GetProperty("member").GetString().ShouldBe("Foo.Bar(int)");
        site.GetProperty("construct").GetString().ShouldBe("if");
    }

    [Fact]
    public async Task require_a_source_root_for_method_level()
    {
        var (exit, _, error) = await Run(await HalfCovered(), "--level", "method");

        exit.ShouldBe(BranchGapCommand.BadInput);
        error.ShouldContain("--level method needs --source-root.");
    }

    [Fact]
    public async Task fail_when_the_source_root_does_not_exist()
    {
        var (exit, _, error) = await Run(await HalfCovered(), "--source-root", "/no/such/dir");

        exit.ShouldBe(BranchGapCommand.BadInput);
        error.ShouldContain("Source root not found");
    }

    [Fact]
    public async Task refuse_fail_under_when_the_reports_hold_no_branch_data()
    {
        var dir = await TempDir(("empty.xml", "<coverage><packages /></coverage>"));

        var (exit, _, error) = await Run(Path.Combine(dir, "empty.xml"), "--fail-under", "80");

        exit.ShouldBe(BranchGapCommand.BadInput);
        error.ShouldContain("No branch data found");
    }

    [Fact]
    public async Task reject_a_nan_fail_under()
    {
        var (exit, _, _) = await Run(await HalfCovered(), "--fail-under", "NaN");

        exit.ShouldNotBe(BranchGapCommand.Success);
    }

    [Fact]
    public async Task expand_wildcards_in_directory_segments_and_mid_path_double_star()
    {
        var dir = await TempDir();
        foreach (var d in new[] { "run1/out", "run2/deep/out", "other/out" })
        {
            Directory.CreateDirectory(Path.Combine(dir, d));
        }

        await File.WriteAllTextAsync(Path.Combine(dir, "run1/out/r.xml"), Cobertura("One", """<line number="1" branch="True" condition-coverage="0% (0/2)" />"""), TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(dir, "run2/deep/out/r.xml"), Cobertura("Two", """<line number="1" branch="True" condition-coverage="0% (0/2)" />"""), TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(dir, "other/out/r.xml"), Cobertura("Other", """<line number="1" branch="True" condition-coverage="0% (0/2)" />"""), TestContext.Current.CancellationToken);

        var (_, output, _) = await Run(Path.Combine(dir, "run*", "**", "out", "r.xml"));

        output.ShouldContain("One");
        output.ShouldContain("Two");
        output.ShouldNotContain("Other");
    }

    [Fact]
    public async Task ignore_branch_counts_that_overflow_instead_of_crashing()
    {
        var dir = await TempDir(("big.xml", Cobertura("Foo", """<line number="1" branch="True" condition-coverage="0% (0/99999999999)" /><line number="2" branch="True" condition-coverage="50% (1/2)" />""")));

        var (exit, output, _) = await Run(Path.Combine(dir, "big.xml"));

        exit.ShouldBe(BranchGapCommand.Success);
        output.ShouldContain("(1/2)");
    }

    [Fact]
    public async Task escape_pipes_and_backticks_in_markdown()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        await File.WriteAllTextAsync(Path.Combine(root, "Ops.cs"), "class Ops\n{\n    public static bool operator |(Ops a, Ops b)\n    {\n        var s = a != null ? \"`x`\" : \"y\";\n        return true;\n    }\n}\n", TestContext.Current.CancellationToken);
        var report = Path.Combine(root, "r.xml");
        await File.WriteAllTextAsync(report, Cobertura("Ops", """<line number="5" branch="True" condition-coverage="50% (1/2)" />""").Replace("Ops.cs", "Ops.cs"), TestContext.Current.CancellationToken);

        var (_, output, _) = await Run(report, "--source-root", root, "--format", "markdown");

        output.ShouldContain("`` var s = a != null ? \"`x`\" : \"y\"; ``");
    }
}
