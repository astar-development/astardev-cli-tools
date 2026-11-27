using AStarDev.BranchGap.Core.Models;
using Shouldly;

namespace AStarDev.BranchGap.Roslyn.TestsUnit;

public class RoslynBranchSiteLocatorShould
{
    private const string Source = """
        namespace N;

        public class Outer
        {
            private readonly int field = Environment.TickCount > 0 ? 1 : 2;

            public int Prop
            {
                get => field;
                set
                {
                    if (value < 0)
                    {
                        throw new ArgumentException("x");
                    }
                }
            }

            public Outer(string? name)
            {
                var n = name ?? "none";
            }

            public int Work(int a, int b)
            {
                if (a > 0 && b > 0)
                {
                    return 1;
                }

                foreach (var i in new[] { 1 })
                {
                }

                return a switch
                {
                    1 => 1,
                    _ => 2,
                };
            }

            public class Inner
            {
                public void Deep(object? o)
                {
                    var s = o?.ToString();
                }
            }
        }
        """;

    private static async Task<IReadOnlyList<BranchSite>> Locate(params int[] lines)
    {
        var path = Path.Combine(Directory.CreateTempSubdirectory().FullName, "Outer.cs");
        await File.WriteAllTextAsync(path, Source, TestContext.Current.CancellationToken);
        return new RoslynBranchSiteLocator().Locate(path, [.. lines.Select(l => new BranchLine(l, 1, 2, path))]);
    }

    private static int LineOf(string fragment) => Source.Split('\n').ToList().FindIndex(l => l.Contains(fragment)) + 1;

    [Fact]
    public async Task name_an_if_inside_a_method_and_keep_the_line_text()
    {
        var site = (await Locate(LineOf("if (a > 0")))[0];

        site.Member.ShouldBe("Outer.Work()");
        site.Construct.ShouldBe("if");
        site.Code.ShouldBe("if (a > 0 && b > 0)");
        (site.Covered, site.Total).ShouldBe((1, 2));
    }

    [Theory]
    [InlineData("foreach (var i", "foreach", "Outer.Work()")]
    [InlineData("return a switch", "switch expression", "Outer.Work()")]
    [InlineData("1 => 1", "switch arm", "Outer.Work()")]
    [InlineData("name ?? ", "??", "Outer..ctor()")]
    [InlineData("if (value < 0)", "if", "Outer.Prop.set")]
    [InlineData("Environment.TickCount", "?:", "Outer.field (initializer)")]
    [InlineData("o?.ToString", "?.", "Outer.Inner.Deep()")]
    public async Task label_the_construct_and_member(string fragment, string construct, string member)
    {
        var site = (await Locate(LineOf(fragment)))[0];

        site.Construct.ShouldBe(construct);
        site.Member.ShouldBe(member);
    }

    [Fact]
    public void return_nothing_when_the_file_cannot_be_read()
    {
        new RoslynBranchSiteLocator().Locate("/no/such/file.cs", [new BranchLine(1, 0, 2)]).Count.ShouldBe(0);
    }

    [Fact]
    public async Task ignore_lines_outside_the_file()
    {
        (await Locate(9999)).Count.ShouldBe(0);
    }
}
