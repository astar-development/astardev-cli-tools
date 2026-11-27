using AStarDev.BranchGap.Core.Models;

namespace AStarDev.BranchGap.Core.Abstractions;

public interface IBranchSiteLocator
{
    /// <summary>Resolves branch lines of one source file to the members and constructs they belong to. Returns nothing when the file cannot be read.</summary>
    IReadOnlyList<BranchSite> Locate(string sourceFile, IReadOnlyList<BranchLine> lines);
}
