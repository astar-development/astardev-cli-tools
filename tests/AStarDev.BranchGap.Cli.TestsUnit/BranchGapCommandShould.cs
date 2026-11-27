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
        output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length.ShouldBe(4);
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
}
