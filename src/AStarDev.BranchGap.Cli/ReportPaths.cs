using AStarDev.BranchGap.Core;

namespace AStarDev.BranchGap.Cli;

public static class ReportPaths
{
    private static readonly EnumerationOptions Plain = new() { IgnoreInaccessible = true, AttributesToSkip = 0 };

    // Symlinks are not followed while recursing for **, so a link back up the tree cannot loop forever.
    private static readonly EnumerationOptions NoLinks = new() { IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint };

    /// <summary>
    /// Expands <c>*</c> and <c>?</c> in any path segment, and <c>**</c> as "any number of folders", so Windows shells, which don't glob, behave like bash.
    /// An argument without wildcards is returned untouched.
    /// </summary>
    public static IReadOnlyList<string> Expand(IEnumerable<string> arguments) =>
        [.. arguments.SelectMany(ExpandOne).Distinct(StringComparer.Ordinal)];

    private static IEnumerable<string> ExpandOne(string argument)
    {
        if (!argument.Contains('*') && !argument.Contains('?'))
        {
            return [argument];
        }

        var root = Path.GetPathRoot(argument) ?? string.Empty;
        var segments = argument[root.Length..].Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);

        return Walk(root.Length > 0 ? root : ".", segments, 0).Order(StringComparer.Ordinal);
    }

    private static IEnumerable<string> Walk(string directory, string[] segments, int index)
    {
        if (!Directory.Exists(directory))
        {
            yield break;
        }

        var segment = segments[index];
        var last = index == segments.Length - 1;

        if (segment == "**")
        {
            if (!last)
            {
                foreach (var found in Walk(directory, segments, index + 1))
                {
                    yield return found;
                }
            }
            else
            {
                segment = "*";
            }

            foreach (var sub in Directory.EnumerateDirectories(directory, "*", NoLinks))
            {
                foreach (var found in Walk(sub, segments, index))
                {
                    yield return found;
                }
            }

            if (!last)
            {
                yield break;
            }
        }

        if (last)
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*", Plain).Where(f => GlobMatcher.IsMatch(segment, Path.GetFileName(f))))
            {
                yield return file;
            }

            yield break;
        }

        foreach (var sub in Directory.EnumerateDirectories(directory, "*", Plain).Where(d => GlobMatcher.IsMatch(segment, Path.GetFileName(d))))
        {
            foreach (var found in Walk(sub, segments, index + 1))
            {
                yield return found;
            }
        }
    }
}
