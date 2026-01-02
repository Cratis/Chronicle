// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;
using Cratis.Chronicle.Projections.DefinitionLanguage.Visitors;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.Parsers;

/// <summary>
/// Parses projection directives using visitor pattern.
/// </summary>
internal class ProjectionDirectiveParser
{
    readonly IDirectiveVisitor[] _visitors =
    [
        new KeyDirectiveVisitor(),
        new AutoMapDirectiveVisitor(),
        new EveryBlockVisitor(),
        new FromEventBlockVisitor(),
        new JoinBlockVisitor(),
        new ChildrenBlockVisitor()
    ];

    public ProjectionDirective? Parse(IParsingContext context)
    {
        foreach (var visitor in _visitors)
        {
            var result = visitor.Visit(context);
            if (result is not null)
            {
                return result;
            }
        }

        context.ReportError($"Unexpected token '{context.Current.Value}' in projection body");
        return null;
    }
}
