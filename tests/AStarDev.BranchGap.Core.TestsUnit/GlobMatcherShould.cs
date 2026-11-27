using Shouldly;

namespace AStarDev.BranchGap.Core.TestsUnit;

public class GlobMatcherShould
{
    [Theory]
    [InlineData("*.UI.*", "App.UI.Views.Home", true)]
    [InlineData("app.ui.*", "App.UI.Views.Home", true)]
    [InlineData("Foo?", "Foo1", true)]
    [InlineData("Foo?", "Foo12", false)]
    [InlineData("Foo", "Foo.Bar", false)]
    [InlineData("a.b", "aXb", false)]
    public void match_star_and_question_mark_case_insensitively(string pattern, string text, bool expected) =>
        GlobMatcher.IsMatch(pattern, text).ShouldBe(expected);

    [Fact]
    public void finish_quickly_on_a_pathological_pattern()
    {
        var text = new string('a', 5000);

        GlobMatcher.IsMatch(string.Concat(Enumerable.Repeat("*a", 200)) + "b", text).ShouldBe(false);
    }
}
