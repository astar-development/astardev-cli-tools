using AStarDev.BranchGap.Core.Abstractions;
using AStarDev.BranchGap.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AStarDev.BranchGap.Roslyn;

public sealed class RoslynBranchSiteLocator : IBranchSiteLocator
{
    public IReadOnlyList<BranchSite> Locate(string sourceFile, IReadOnlyList<BranchLine> lines)
    {
        string text;
        try
        {
            text = File.ReadAllText(sourceFile);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }

        var tree = CSharpSyntaxTree.ParseText(text);
        var root = tree.GetRoot();
        var source = tree.GetText();

        return [.. lines
            .Where(l => l.LineNumber >= 1 && l.LineNumber <= source.Lines.Count)
            .Select(l => ToSite(sourceFile, root, source.Lines[l.LineNumber - 1], l))];
    }

    private static BranchSite ToSite(string sourceFile, SyntaxNode root, TextLine line, BranchLine branchLine)
    {
        var construct = FindConstruct(root, line.Span);
        var anchor = construct ?? root.FindToken(line.Span.Start).Parent ?? root;

        return new BranchSite(sourceFile, branchLine.LineNumber, MemberName(anchor), construct is null ? "branch" : Label(construct)!, line.ToString().Trim(), branchLine.Covered, branchLine.Total);
    }

    // Constructs that start on the line (outermost first); else the nearest one that encloses it, e.g. a continuation line of a multi-line condition.
    private static SyntaxNode? FindConstruct(SyntaxNode root, TextSpan span) =>
        root.DescendantNodes(span).Where(n => Label(n) is not null && span.Contains(n.SpanStart)).OrderBy(n => n.SpanStart).FirstOrDefault()
        ?? root.FindToken(span.Start).Parent?.AncestorsAndSelf().FirstOrDefault(n => Label(n) is not null);

    private static string? Label(SyntaxNode node) => node.Kind() switch
    {
        SyntaxKind.IfStatement => "if",
        SyntaxKind.ElseClause => "else",
        SyntaxKind.ConditionalExpression => "?:",
        SyntaxKind.SwitchStatement => "switch",
        SyntaxKind.SwitchExpression => "switch expression",
        SyntaxKind.SwitchExpressionArm => "switch arm",
        SyntaxKind.CaseSwitchLabel or SyntaxKind.CasePatternSwitchLabel or SyntaxKind.DefaultSwitchLabel => "case",
        SyntaxKind.CoalesceExpression => "??",
        SyntaxKind.CoalesceAssignmentExpression => "??=",
        SyntaxKind.LogicalAndExpression => "&&",
        SyntaxKind.LogicalOrExpression => "||",
        SyntaxKind.ConditionalAccessExpression => "?.",
        SyntaxKind.WhileStatement => "while",
        SyntaxKind.DoStatement => "do",
        SyntaxKind.ForStatement => "for",
        SyntaxKind.ForEachStatement => "foreach",
        SyntaxKind.CatchClause => "catch",
        SyntaxKind.UsingStatement => "using",
        SyntaxKind.AwaitExpression => "await",
        _ => null,
    };

    private static string MemberName(SyntaxNode node)
    {
        var member = node.AncestorsAndSelf().FirstOrDefault(a => a is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or BasePropertyDeclarationSyntax or BaseFieldDeclarationSyntax);
        var type = string.Join('.', node.Ancestors().OfType<BaseTypeDeclarationSyntax>().Select(t => t.Identifier.Text).Reverse());

        var name = member switch
        {
            MethodDeclarationSyntax m => m.Identifier.Text + "()",
            ConstructorDeclarationSyntax c => (c.Modifiers.Any(SyntaxKind.StaticKeyword) ? ".cctor" : ".ctor") + "()",
            DestructorDeclarationSyntax => "Finalize()",
            OperatorDeclarationSyntax o => "operator " + o.OperatorToken.Text,
            ConversionOperatorDeclarationSyntax => "conversion operator",
            AccessorDeclarationSyntax a => PropertyName(a.Parent?.Parent) + "." + a.Keyword.Text,
            PropertyDeclarationSyntax p => p.Identifier.Text,
            IndexerDeclarationSyntax => "this[]",
            EventDeclarationSyntax e => e.Identifier.Text,
            BaseFieldDeclarationSyntax f => f.Declaration is { } d ? d.Variables[0].Identifier.Text + " (initializer)" : "field",
            _ => "(type level)",
        };

        return type.Length == 0 ? name : $"{type}.{name}";
    }

    private static string PropertyName(SyntaxNode? node) => node switch
    {
        PropertyDeclarationSyntax p => p.Identifier.Text,
        IndexerDeclarationSyntax => "this[]",
        EventDeclarationSyntax e => e.Identifier.Text,
        _ => "?",
    };
}
