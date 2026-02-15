// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.DefinitionLanguage.AST;

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.Visitors;

/// <summary>
/// Visitor for parsing automap directives.
/// </summary>
public class AutoMapDirectiveVisitor : IDirectiveVisitor
{
    /// <inheritdoc/>
    public ProjectionDirective? Visit(IParsingContext context)
    {
        if (!context.Check(TokenType.AutoMap))
        {
            return null;
        }

        context.Advance();
        return new AutoMapDirective();
    }
}
