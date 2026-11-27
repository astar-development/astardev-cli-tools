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

        site.Member.ShouldBe("Outer.Work(int, int)");
        site.Construct.ShouldBe("if");
        site.Code.ShouldBe("if (a > 0 && b > 0)");
        (site.Covered, site.Total).ShouldBe((1, 2));
    }

    [Theory]
    [InlineData("foreach (var i", "foreach", "Outer.Work(int, int)")]
    [InlineData("return a switch", "switch expression", "Outer.Work(int, int)")]
    [InlineData("1 => 1", "switch arm", "Outer.Work(int, int)")]
    [InlineData("name ?? ", "??", "Outer..ctor(string?)")]
    [InlineData("if (value < 0)", "if", "Outer.Prop.set")]
    [InlineData("Environment.TickCount", "?:", "Outer.field (initializer)")]
    [InlineData("o?.ToString", "?.", "Outer.Inner.Deep(object?)")]
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

    [Fact]
    public async Task keep_overloads_and_constructors_apart_by_parameter_types()
    {
        var path = Path.Combine(Directory.CreateTempSubdirectory().FullName, "O.cs");
        await File.WriteAllTextAsync(path, "class O\n{\n    O(int a) { if (a > 0) { } }\n    O(string s) { if (s is null) { } }\n    void M(int a) { if (a > 0) { } }\n    void M(long a) { if (a > 0) { } }\n}\n", TestContext.Current.CancellationToken);

        var sites = new RoslynBranchSiteLocator().Locate(path, [new BranchLine(3, 0, 2), new BranchLine(4, 0, 2), new BranchLine(5, 0, 2), new BranchLine(6, 0, 2)]);

        sites.Select(s => s.Member).ShouldBe(["O..ctor(int)", "O..ctor(string)", "O.M(int)", "O.M(long)"]);
    }

    [Fact]
    public async Task locate_the_same_file_repeatedly()
    {
        var path = Path.Combine(Directory.CreateTempSubdirectory().FullName, "Outer.cs");
        await File.WriteAllTextAsync(path, Source, TestContext.Current.CancellationToken);
        var locator = new RoslynBranchSiteLocator();

        var first = locator.Locate(path, [new BranchLine(LineOf("if (a > 0"), 1, 2)]);
        var second = locator.Locate(path, [new BranchLine(LineOf("foreach (var i"), 1, 2)]);

        first[0].Construct.ShouldBe("if");
        second[0].Construct.ShouldBe("foreach");
    }
}
