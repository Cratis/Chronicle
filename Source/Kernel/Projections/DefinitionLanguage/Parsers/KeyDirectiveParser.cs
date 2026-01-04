// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;
using Cratis.Chronicle.Projections.DefinitionLanguage.Visitors;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.Parsers;

/// <summary>
/// Parses key directives (simple and composite).
/// </summary>
internal sealed class KeyDirectiveParser
{
    readonly TypeRefParser _typeRefs = new();
    readonly ExpressionParser _expressions = new();

    /// <summary>
    /// Parses a key directive from the given context.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <returns>The parsed projection directive, or null if parsing failed.</returns>
    public ProjectionDirective? Parse(IParsingContext context)
    {
        context.Advance(); // Skip 'key'

        // Check if next token is an identifier followed by indent (composite key with type name)
        if (context.Check(TokenType.Identifier) && context.Peek().Type == TokenType.Indent)
        {
            var typeName = _typeRefs.Parse(context);
            if (typeName is null) return null;

            if (context.Expect(TokenType.Indent) is null) return null;

            var parts = new List<KeyPart>();
            while (!context.Check(TokenType.Dedent) && !context.IsAtEnd)
            {
                var propNameToken = context.Expect(TokenType.Identifier);
                if (propNameToken is null) continue;

                if (context.Expect(TokenType.Equals) is null) continue;

                var expr = _expressions.Parse(context);
                if (expr is not null)
                {
                    parts.Add(new KeyPart(propNameToken.Value, expr));
                }
            }

            context.Expect(TokenType.Dedent);

            return new CompositeKeyDirective(typeName, parts);
        }

        var keyExpr = _expressions.Parse(context);
        return keyExpr is not null ? new KeyDirective(keyExpr) : null;
    }
}
