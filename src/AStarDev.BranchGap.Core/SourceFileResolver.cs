namespace AStarDev.BranchGap.Core;

public static class SourceFileResolver
{
    /// <summary>
    /// Finds a source file named in a report that may have been produced on another machine, and never returns a file outside <paramref name="sourceRoot"/>:
    /// the longest tail of the reported path that exists under the root, else the reported path itself when it already lies inside the root.
    /// </summary>
    public static string? Resolve(string reportedPath, string sourceRoot)
    {
        if (reportedPath.Length == 0)
        {
            return null;
        }

        var root = Path.GetFullPath(sourceRoot);
        var segments = reportedPath.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries).Where(s => s is not ("." or "..")).ToArray();
        for (var start = 0; start < segments.Length; start++)
        {
            var candidate = Path.GetFullPath(Path.Combine([root, .. segments[start..]]));
            if (IsInside(root, candidate) && File.Exists(candidate))
            {
                return candidate;
            }
        }

        var asWritten = Path.GetFullPath(reportedPath);
        return IsInside(root, asWritten) && File.Exists(asWritten) ? asWritten : null;
    }

    private static bool IsInside(string root, string path) =>
        path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.Ordinal);
}
