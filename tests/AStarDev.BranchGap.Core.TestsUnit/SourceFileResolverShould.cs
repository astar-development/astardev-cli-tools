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
    public void use_the_reported_path_when_it_exists_inside_the_root()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        var file = MakeFile(root, "a.cs");

        SourceFileResolver.Resolve(file, root).ShouldBe(file);
    }

    [Fact]
    public void never_return_a_file_outside_the_root_even_when_the_report_names_it()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        var outside = MakeFile(Directory.CreateTempSubdirectory().FullName, "secret.cs");

        SourceFileResolver.Resolve(outside, root).ShouldBe(null);
    }

    [Fact]
    public void ignore_dot_dot_segments_so_the_root_cannot_be_escaped()
    {
        var parent = Directory.CreateTempSubdirectory().FullName;
        var root = Path.Combine(parent, "src");
        Directory.CreateDirectory(root);
        MakeFile(parent, "secret.cs");

        SourceFileResolver.Resolve("../secret.cs", root).ShouldBe(null);
    }

    [Fact]
    public void prefer_the_copy_under_the_root_over_a_relative_path_in_the_working_directory()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        var file = MakeFile(root, Path.Combine("Proj", "Foo.cs"));

        SourceFileResolver.Resolve("Proj/Foo.cs", root).ShouldBe(file);
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
