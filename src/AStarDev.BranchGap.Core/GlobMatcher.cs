using System.Text.RegularExpressions;

namespace AStarDev.BranchGap.Core;

public static class GlobMatcher
{
    /// <summary>Case-insensitive match where <c>*</c> is any run of characters and <c>?</c> is one character.</summary>
    public static bool IsMatch(string pattern, string text) =>
        Regex.IsMatch(text, "^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
}
