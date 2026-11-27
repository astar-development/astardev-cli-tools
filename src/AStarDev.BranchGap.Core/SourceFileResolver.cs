namespace AStarDev.BranchGap.Core;

public static class SourceFileResolver
{
    /// <summary>
    /// Finds a source file named in a report that may have been produced on another machine: the path as written, else the longest tail of it that exists under <paramref name="sourceRoot"/>.
    /// </summary>
    public static string? Resolve(string reportedPath, string sourceRoot)
    {
        if (reportedPath.Length == 0)
        {
            return null;
        }

        if (File.Exists(reportedPath))
        {
            return reportedPath;
        }

        var segments = reportedPath.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        for (var start = 0; start < segments.Length; start++)
        {
            var candidate = Path.Combine([sourceRoot, .. segments[start..]]);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
