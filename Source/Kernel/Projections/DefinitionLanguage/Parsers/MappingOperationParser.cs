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
            var propToken = context.Expect(TokenType.Identifier);
            if (propToken is null) return null;
            return new IncrementOperation(propToken.Value)
            {
                Line = token.Line,
                Column = token.Column
            };
        }

        if (context.Check(TokenType.Decrement))
        {
            var token = context.Current;
            context.Advance();
            var propToken = context.Expect(TokenType.Identifier);
            if (propToken is null) return null;
            return new DecrementOperation(propToken.Value)
            {
                Line = token.Line,
                Column = token.Column
            };
        }

        if (context.Check(TokenType.Count))
        {
            var token = context.Current;
            context.Advance();
            var propToken = context.Expect(TokenType.Identifier);
            if (propToken is null) return null;
            return new CountOperation(propToken.Value)
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
}
