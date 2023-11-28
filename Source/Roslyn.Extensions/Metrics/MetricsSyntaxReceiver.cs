using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Roslyn.Extensions.Metrics;

public class MetricsSyntaxReceiver : ISyntaxReceiver
{
    readonly List<ClassDeclarationSyntax> _candidates = new();

    internal IEnumerable<ClassDeclarationSyntax> Candidates => _candidates;

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is not ClassDeclarationSyntax classSyntax) return;

        if (classSyntax.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)) &&
            classSyntax.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword)))
        {
            if (classSyntax.Members.Any(member =>
                member.IsKind(SyntaxKind.MethodDeclaration) &&
                member.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)) &&
                member.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword))))
            {
                _candidates.Add(classSyntax);
            }
        }
    }
}