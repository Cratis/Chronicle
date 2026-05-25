// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures.Engine.DeclarationLanguage.AST;
using Cratis.Chronicle.Captures.Engine.DeclarationLanguage.Visitors;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.Parsers;

/// <summary>
/// Parses when clauses.
/// </summary>
public class WhenClauseParser
{
    /// <summary>
    /// Parses a when clause from the given context.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <returns>The parsed when clause, or null if parsing failed.</returns>
    public WhenClauseNode? Parse(IParsingContext context)
    {
        var whenToken = context.Expect(TokenType.When);
        if (whenToken is null)
        {
            return null;
        }

        if (context.Check(TokenType.Added))
        {
            context.Advance();
            return CreateNode(WhenClauseType.Added, whenToken, []);
        }

        if (context.Check(TokenType.Removed))
        {
            context.Advance();
            return CreateNode(WhenClauseType.Removed, whenToken, []);
        }

        if (context.Check(TokenType.TemplateLiteral))
        {
            var expression = context.Current.Value;
            context.Advance();
            return new WhenClauseNode(WhenClauseType.Expression, [], Expression: expression)
            {
                Line = whenToken.Line,
                Column = whenToken.Column
            };
        }

        var property = ParsingHelpers.ParsePropertyPath(context);
        if (property is null)
        {
            return null;
        }

        if (context.Check(TokenType.From) && context.Current.Line == whenToken.Line)
        {
            context.Advance();
            var fromValue = ParsingHelpers.ParseSimpleValue(context);
            if (fromValue is null)
            {
                return null;
            }

            if (context.Expect(TokenType.To) is null)
            {
                return null;
            }

            var toValue = ParsingHelpers.ParseSimpleValue(context);
            if (toValue is null)
            {
                return null;
            }

            return new WhenClauseNode(WhenClauseType.ValueTransition, [property], fromValue, toValue)
            {
                Line = whenToken.Line,
                Column = whenToken.Column
            };
        }

        if (context.Check(TokenType.Or) && context.Current.Line == whenToken.Line)
        {
            var properties = new List<string> { property };
            while (context.Check(TokenType.Or) && context.Current.Line == whenToken.Line)
            {
                context.Advance();
                var next = ParsingHelpers.ParsePropertyPath(context);
                if (next is null)
                {
                    return null;
                }

                properties.Add(next);
            }

            return CreateNode(WhenClauseType.LogicalOr, whenToken, properties);
        }

        if (context.Check(TokenType.And) && context.Current.Line == whenToken.Line)
        {
            var properties = new List<string> { property };
            while (context.Check(TokenType.And) && context.Current.Line == whenToken.Line)
            {
                context.Advance();
                var next = ParsingHelpers.ParsePropertyPath(context);
                if (next is null)
                {
                    return null;
                }

                properties.Add(next);
            }

            return CreateNode(WhenClauseType.LogicalAnd, whenToken, properties);
        }

        return CreateNode(WhenClauseType.PropertyChange, whenToken, [property]);
    }

    static WhenClauseNode CreateNode(WhenClauseType type, Token token, IReadOnlyList<string> properties) => new(type, properties)
    {
        Line = token.Line,
        Column = token.Column
    };
}
