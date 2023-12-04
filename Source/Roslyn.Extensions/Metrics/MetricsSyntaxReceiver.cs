// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Roslyn.Extensions.Metrics;

/// <summary>
/// Represents the syntax receiver for metrics.
/// </summary>
public class MetricsSyntaxReceiver : ISyntaxReceiver
{
    readonly List<ClassDeclarationSyntax> _candidates = new();

    /// <summary>
    /// Gets the candidates for code generation.
    /// </summary>
    internal IEnumerable<ClassDeclarationSyntax> Candidates => _candidates;

    /// <inheritdoc/>
    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is not ClassDeclarationSyntax classSyntax) return;

        if (classSyntax.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)) &&
            classSyntax.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword)) && classSyntax.Members.Any(member =>
                member.IsKind(SyntaxKind.MethodDeclaration) &&
                member.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)) &&
                member.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword))))
        {
            _candidates.Add(classSyntax);
        }
    }
}
