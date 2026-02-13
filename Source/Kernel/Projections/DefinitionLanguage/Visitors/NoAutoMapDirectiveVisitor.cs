// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.Visitors;

/// <summary>
/// Visitor for parsing the "no automap" directive.
/// </summary>
public class NoAutoMapDirectiveVisitor : IDirectiveVisitor
{
    /// <inheritdoc/>
    public ProjectionDirective? Visit(IParsingContext context)
    {
        if (!context.Check(TokenType.No))
        {
            return null;
        }

        context.Advance(); // Skip 'no'

        if (context.Expect(TokenType.AutoMap) is null)
        {
            return null;
        }

        return new NoAutoMapDirective();
    }
}
