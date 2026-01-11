// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;
using Cratis.Chronicle.Projections.DefinitionLanguage.Visitors;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.Parsers;

/// <summary>
/// Handles parsing of mapping operations.
/// </summary>
public class MappingOperationParser
{
    readonly ExpressionParser _expressions = new();

    /// <summary>
    /// Parses a mapping operation.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <returns>The parsed mapping operation or null.</returns>
    public MappingOperation? Parse(IParsingContext context)
    {
        if (context.Check(TokenType.Increment))
        {
            var token = context.Current;
            context.Advance();
            var propPath = ParsePropertyPath(context);
            if (propPath is null) return null;
            return new IncrementOperation(propPath)
            {
                Line = token.Line,
                Column = token.Column
            };
        }

        if (context.Check(TokenType.Decrement))
        {
            var token = context.Current;
            context.Advance();
            var propPath = ParsePropertyPath(context);
            if (propPath is null) return null;
            return new DecrementOperation(propPath)
            {
                Line = token.Line,
                Column = token.Column
            };
        }

        if (context.Check(TokenType.Count))
        {
            var token = context.Current;
            context.Advance();
            var propPath = ParsePropertyPath(context);
            if (propPath is null) return null;
            return new CountOperation(propPath)
            {
                Line = token.Line,
                Column = token.Column
            };
        }

        if (context.Check(TokenType.Add))
        {
            var token = context.Current;
            context.Advance();
            var propToken = context.Expect(TokenType.Identifier);
            if (propToken is null) return null;

            if (context.Expect(TokenType.By) is null) return null;

            var value = _expressions.Parse(context);
            if (value is null) return null;
            return new AddOperation(propToken.Value, value)
            {
                Line = token.Line,
                Column = token.Column
            };
        }

        if (context.Check(TokenType.Subtract))
        {
            var token = context.Current;
            context.Advance();
            var propToken = context.Expect(TokenType.Identifier);
            if (propToken is null) return null;

            if (context.Expect(TokenType.By) is null) return null;

            var value = _expressions.Parse(context);
            if (value is null) return null;
            return new SubtractOperation(propToken.Value, value)
            {
                Line = token.Line,
                Column = token.Column
            };
        }

        // Assignment
        if (context.Check(TokenType.Identifier))
        {
            var propNameToken = context.Expect(TokenType.Identifier);
            if (propNameToken is null) return null;

            if (context.Expect(TokenType.Equals) is null) return null;

            var value = _expressions.Parse(context);
            if (value is null) return null;
            return new AssignmentOperation(propNameToken.Value, value)
            {
                Line = propNameToken.Line,
                Column = propNameToken.Column
            };
        }

        context.ReportError("Expected mapping operation");
        return null;
    }

    string? ParsePropertyPath(IParsingContext context)
    {
        var propToken = context.Expect(TokenType.Identifier);
        if (propToken is null) return null;

        var propertyPath = propToken.Value;

        // Check for dynamic dictionary key expression like: theDictionary.$eventContext.type.id
        while (context.Check(TokenType.Dot))
        {
            context.Advance(); // Skip dot

            // Check if next token is a $ (expression)
            if (context.Check(TokenType.Dollar))
            {
                context.Advance(); // Skip $
                var nameToken = context.Expect(TokenType.Identifier);
                if (nameToken is null) return null;

                propertyPath += ".$" + nameToken.Value;

                // Continue building the expression path
                while (context.Check(TokenType.Dot))
                {
                    context.Advance();
                    var nextToken = context.Expect(TokenType.Identifier);
                    if (nextToken is null) return null;
                    propertyPath += "." + nextToken.Value;
                }
            }
            else
            {
                // Regular property path
                var nextToken = context.Expect(TokenType.Identifier);
                if (nextToken is null) return null;
                propertyPath += "." + nextToken.Value;
            }
        }

        return propertyPath;
    }
}
