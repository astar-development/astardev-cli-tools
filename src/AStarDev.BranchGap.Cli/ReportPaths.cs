namespace AStarDev.BranchGap.Cli;

public static class ReportPaths
{
    /// <summary>Expands <c>*</c>/<c>?</c> in the file name (and <c>**</c> in the directory part, meaning "search subfolders") so Windows shells, which don't glob, behave like bash.</summary>
    // shortcut: wildcards only in the last path segment (plus ** for recursion); upgrade to Microsoft.Extensions.FileSystemGlobbing if richer patterns are needed.
    public static IReadOnlyList<string> Expand(IEnumerable<string> arguments) =>
        [.. arguments.SelectMany(ExpandOne).Distinct(StringComparer.Ordinal)];

    private static IEnumerable<string> ExpandOne(string argument)
    {
        var fileName = Path.GetFileName(argument);
        if (!fileName.Contains('*') && !fileName.Contains('?'))
        {
            return [argument];
        }

        var directory = Path.GetDirectoryName(argument) ?? string.Empty;
        var recursive = directory.Contains("**", StringComparison.Ordinal);
        var root = recursive ? directory[..directory.IndexOf("**", StringComparison.Ordinal)].TrimEnd('/', '\\') : directory;
        root = root.Length == 0 ? "." : root;

        return Directory.Exists(root)
            ? Directory.EnumerateFiles(root, fileName, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).Order(StringComparer.Ordinal)
            : [];
    }
}
