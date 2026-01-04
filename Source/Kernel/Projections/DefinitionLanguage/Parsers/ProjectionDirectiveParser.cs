// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;
using Cratis.Chronicle.Projections.DefinitionLanguage.Visitors;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.Parsers;

/// <summary>
/// Parses projection directives using visitor pattern.
/// </summary>
public class ProjectionDirectiveParser
{
    readonly IDirectiveVisitor[] _visitors =
    [
        new SequenceDirectiveVisitor(),
        new KeyDirectiveVisitor(),
        new AutoMapDirectiveVisitor(),
        new NoAutoMapDirectiveVisitor(),
        new EveryBlockVisitor(),
        new FromEventBlockVisitor(),
        new JoinBlockVisitor(),
        new ChildrenBlockVisitor(),
        new RemoveWithDirectiveVisitor()
    ];

    /// <summary>
    /// Parses a projection directive from the given context.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <returns>The parsed projection directive, or null if parsing failed.</returns>
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
