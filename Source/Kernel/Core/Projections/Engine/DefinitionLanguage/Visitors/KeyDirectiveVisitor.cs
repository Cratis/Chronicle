// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.DefinitionLanguage.AST;
using Cratis.Chronicle.Projections.Engine.DefinitionLanguage.Parsers;

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.Visitors;

/// <summary>
/// Visitor for parsing key directives (simple and composite keys).
/// </summary>
internal sealed class KeyDirectiveVisitor : IDirectiveVisitor
{
    readonly KeyDirectiveParser _parser = new();

    /// <inheritdoc/>
    public ProjectionDirective? Visit(IParsingContext context)
    {
        if (!context.Check(TokenType.Key))
        {
            return null;
        }

        return _parser.Parse(context);
    }
}
