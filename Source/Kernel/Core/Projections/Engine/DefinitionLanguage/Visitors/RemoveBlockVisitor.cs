// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.DefinitionLanguage.AST;
using Cratis.Chronicle.Projections.Engine.DefinitionLanguage.Parsers;

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.Visitors;

/// <summary>
/// Visitor for parsing remove blocks.
/// </summary>
internal sealed class RemoveBlockVisitor
{
    readonly TypeRefParser _typeRefs = new();
    readonly ExpressionParser _expressions = new();

    /// <summary>
    /// Visits and parses a remove block.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <returns>The parsed child block or null if not applicable.</returns>
    public ChildBlock? Visit(IParsingContext context)
    {
        if (!context.Check(TokenType.Remove))
        {
            return null;
        }

        context.Advance(); // Skip 'remove'

        // Check for "via join"
        if (context.Check(TokenType.Via))
        {
            context.Advance();
            if (context.Expect(TokenType.Join) is null) return null;
            if (context.Expect(TokenType.On) is null) return null;

            var eventType = _typeRefs.Parse(context);
            if (eventType is null) return null;

            Expression? key = null;
            if (context.Check(TokenType.Key))
            {
                context.Advance();
                key = _expressions.Parse(context);
            }

            return new RemoveViaJoinBlock(eventType, key);
        }

        if (context.Expect(TokenType.With) is null) return null;

        var removeEventType = _typeRefs.Parse(context);
        if (removeEventType is null) return null;

        Expression? removeKey = null;
        if (context.Check(TokenType.Key))
        {
            context.Advance();
            removeKey = _expressions.Parse(context);
        }

        if (context.Expect(TokenType.Indent) is null) return null;

        Expression? parentKey = null;
        while (!context.Check(TokenType.Dedent) && !context.IsAtEnd)
        {
            if (context.Check(TokenType.Parent))
            {
                context.Advance();
                parentKey = _expressions.Parse(context);
            }
            else
            {
                context.ReportError($"Unexpected token '{context.Current.Value}' in remove block");
                context.Advance(); // Skip invalid token to continue parsing
            }
        }

        context.Expect(TokenType.Dedent);
        return new RemoveBlock(removeEventType, removeKey, parentKey);
    }
}
