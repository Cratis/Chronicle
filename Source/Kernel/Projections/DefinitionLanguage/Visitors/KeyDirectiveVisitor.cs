// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;
using Cratis.Chronicle.Projections.DefinitionLanguage.Parsers;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.Visitors;

/// <summary>
/// Visitor for parsing key directives (simple and composite keys).
/// </summary>
internal class KeyDirectiveVisitor : IDirectiveVisitor
{
    readonly KeyDirectiveParser _parser = new();

    public ProjectionDirective? Visit(IParsingContext context)
    {
        if (!context.Check(TokenType.Key))
        {
            return null;
        }

        return _parser.Parse(context);
    }
}
