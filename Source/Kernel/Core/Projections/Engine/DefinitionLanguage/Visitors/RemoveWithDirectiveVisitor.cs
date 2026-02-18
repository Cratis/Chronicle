// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.DefinitionLanguage.AST;
using Cratis.Chronicle.Projections.Engine.DefinitionLanguage.Parsers;

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.Visitors;

/// <summary>
/// Visitor for parsing projection-level remove directives.
/// </summary>
public class RemoveWithDirectiveVisitor : IDirectiveVisitor
{
    readonly TypeRefParser _typeRefs = new();
    readonly ExpressionParser _expressions = new();

    /// <inheritdoc/>
    public ProjectionDirective? Visit(IParsingContext context)
    {
        if (!context.Check(TokenType.Remove))
        {
            return null;
        }

        var removeToken = context.Current;
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

            return new RemoveWithJoinDirective(eventType, key)
            {
                Line = removeToken.Line,
                Column = removeToken.Column
            };
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

        return new RemoveWithDirective(removeEventType, removeKey)
        {
            Line = removeToken.Line,
            Column = removeToken.Column
        };
    }
}
