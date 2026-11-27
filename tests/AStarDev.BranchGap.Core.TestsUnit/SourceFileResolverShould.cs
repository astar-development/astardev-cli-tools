using Shouldly;

namespace AStarDev.BranchGap.Core.TestsUnit;

public class SourceFileResolverShould
{
    private static string MakeFile(string root, string relative)
    {
        var path = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "x");
        return path;
    }

    [Fact]
    public void use_the_reported_path_when_it_exists()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        var file = MakeFile(root, "a.cs");

        SourceFileResolver.Resolve(file, "/elsewhere").ShouldBe(file);
    }

    [Fact]
    public void find_the_longest_matching_tail_under_the_source_root()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        var file = MakeFile(root, Path.Combine("Proj", "Sub", "Foo.cs"));

        SourceFileResolver.Resolve("/home/other/repos/x/Proj/Sub/Foo.cs", root).ShouldBe(file);
        SourceFileResolver.Resolve(@"C:\build\agent\Proj\Sub\Foo.cs", root).ShouldBe(file);
    }

    [Fact]
    public void return_null_when_nothing_matches_or_the_path_is_empty()
    {
        var root = Directory.CreateTempSubdirectory().FullName;

        SourceFileResolver.Resolve("/a/b/Missing.cs", root).ShouldBe(null);
        SourceFileResolver.Resolve(string.Empty, root).ShouldBe(null);
    }
}
