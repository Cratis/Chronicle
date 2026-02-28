// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.DefinitionLanguage.AST;
using Cratis.Chronicle.Projections.Engine.DefinitionLanguage.Visitors;

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.Parsers;

/// <summary>
/// Handles parsing of type references.
/// </summary>
public class TypeRefParser
{
    /// <summary>
    /// Parses a type reference.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <returns>The parsed type reference or null.</returns>
    public TypeRef? Parse(IParsingContext context)
    {
        var identifierToken = context.Expect(TokenType.Identifier);
        if (identifierToken is null) return null;

        var parts = new List<string> { identifierToken.Value };

        while (context.Check(TokenType.Dot))
        {
            context.Advance();
            var nextToken = context.Expect(TokenType.Identifier);
            if (nextToken is null) return null;
            parts.Add(nextToken.Value);
        }

        return new TypeRef(string.Join('.', parts))
        {
            Line = identifierToken.Line,
            Column = identifierToken.Column
        };
    }
}
