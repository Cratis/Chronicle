// Copyright (c) Cratis. All rights reserved.
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
    static readonly string[] _metricsAttributes =
    [
        "Counter",
        "Measurement"
    ];

    readonly List<ClassDeclarationSyntax> _candidates = [];

    /// <summary>
    /// Gets the candidates for code generation.
    /// </summary>
    internal IEnumerable<ClassDeclarationSyntax> Candidates => _candidates;

    /// <inheritdoc/>
    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is not ClassDeclarationSyntax classSyntax) return;

        if (classSyntax.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)) &&
            classSyntax.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword)) &&
            classSyntax.Members.Any(member => member.IsKind(SyntaxKind.MethodDeclaration) &&
                HasMetricsAttribute(member) &&
                member.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)) &&
                member.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword))))
        {
            _candidates.Add(classSyntax);
        }
    }

    bool HasMetricsAttribute(MemberDeclarationSyntax memberSyntax) => memberSyntax
                            .AttributeLists
                            .SelectMany(_ => _.Attributes)
                            .Any(_ => _metricsAttributes
                                .Any(m => _.Name.ToString().StartsWith(m)));
}
