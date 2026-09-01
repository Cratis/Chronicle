// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.CSharp;

/// <summary>
/// Puts each parameter of a record on its own line.
/// </summary>
/// <param name="indentation">How many spaces each parameter is indented by.</param>
/// <remarks>
/// Roslyn's <c>NormalizeWhitespace</c> lays a parameter list out on one line however long it gets. A
/// model-bound read model carries its projection in attributes on those parameters, so that one line
/// holds the whole projection - unreadable for anything past a couple of properties, and impossible
/// to see which attribute belongs to which property.
/// </remarks>
public class RecordParameterFormatter(int indentation) : CSharpSyntaxRewriter
{
    /// <inheritdoc/>
    public override SyntaxNode? VisitParameterList(ParameterListSyntax node)
    {
        var visited = (ParameterListSyntax)base.VisitParameterList(node)!;

        if (visited.Parent is not RecordDeclarationSyntax || visited.Parameters.Count == 0)
        {
            return visited;
        }

        var leading = new[] { ElasticCarriageReturnLineFeed, Whitespace(new string(' ', indentation)) };
        var parameters = visited.Parameters.Select(parameter => parameter.WithLeadingTrivia(leading));

        return visited.WithParameters(SeparatedList(parameters, visited.Parameters.GetSeparators()));
    }
}
