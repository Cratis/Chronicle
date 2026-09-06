// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.CSharp;

/// <summary>
/// Puts each step of the builder chain on its own line.
/// </summary>
/// <param name="rootIdentifier">The identifier the chain to break hangs off.</param>
/// <param name="indentation">How many spaces each step of the chain is indented by.</param>
/// <remarks>
/// <para>
/// Roslyn's <c>NormalizeWhitespace</c> lays a chained call out as a single line however long it gets,
/// which for a projection of any size is one unreadable run of <c>.From&lt;&gt;().From&lt;&gt;()</c>.
/// Breaking before the dot is the convention every hand-written projection follows, so this walks the
/// normalized tree and does the same - on the tokens rather than the rendered text, so it cannot
/// break a string that happens to contain a dot.
/// </para>
/// <para>
/// Only the chain hanging off the builder is broken. The short chains inside a step's lambda -
/// <c>.Set(...).To(...)</c> - read better kept together, which is also how the other languages'
/// generators lay them out.
/// </para>
/// </remarks>
public class FluentChainFormatter(string rootIdentifier, int indentation) : CSharpSyntaxRewriter
{
    /// <inheritdoc/>
    public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        var visited = (MemberAccessExpressionSyntax)base.VisitMemberAccessExpression(node)!;

        // Every step starts a new line, including the first, so the builder is left alone on its line
        // and the steps line up under one another rather than the first hanging off the signature.
        var isChainStep = visited.Expression is InvocationExpressionSyntax
            || (visited.Expression is IdentifierNameSyntax root
                && string.Equals(root.Identifier.Text, rootIdentifier, StringComparison.Ordinal));

        return isChainStep && RootsAtBuilder(visited)
            ? visited.WithOperatorToken(visited.OperatorToken.WithLeadingTrivia(
                ElasticCarriageReturnLineFeed,
                Whitespace(new string(' ', indentation))))
            : visited;
    }

    bool RootsAtBuilder(ExpressionSyntax expression)
    {
        var current = expression;

        while (true)
        {
            switch (current)
            {
                case MemberAccessExpressionSyntax memberAccess:
                    current = memberAccess.Expression;
                    break;
                case InvocationExpressionSyntax invocation:
                    current = invocation.Expression;
                    break;
                case IdentifierNameSyntax identifier:
                    return string.Equals(identifier.Identifier.Text, rootIdentifier, StringComparison.Ordinal);
                default:
                    return false;
            }
        }
    }
}
