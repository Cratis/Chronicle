// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;
using Cratis.Chronicle.Projections.DefinitionLanguage.Visitors;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.Parsers;

/// <summary>
/// Handles parsing of expressions.
/// </summary>
public class ExpressionParser
{
    /// <summary>
    /// Parses an expression.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <returns>The parsed expression or null.</returns>
    public Expression? Parse(IParsingContext context)
    {
        // Template literal
        if (context.Check(TokenType.TemplateLiteral))
        {
            var template = context.Current.Value;
            context.Advance();
            return ParseTemplate(context, template);
        }

        // $eventSourceId or $eventContext.property
        if (context.Check(TokenType.Dollar))
        {
            context.Advance();
            var nameToken = context.Expect(TokenType.Identifier);
            if (nameToken is null) return null;
            var name = nameToken.Value;

            if (name.Equals("eventSourceId", StringComparison.OrdinalIgnoreCase))
            {
                return new EventSourceIdExpression();
            }

            if (name.Equals("eventContext", StringComparison.OrdinalIgnoreCase))
            {
                if (context.Expect(TokenType.Dot) is null) return null;
                var propertyToken = context.Expect(TokenType.Identifier);
                if (propertyToken is null) return null;
                return new EventContextExpression(propertyToken.Value);
            }

            context.Errors.Add($"Unknown expression '${name}'", context.Current.Line, context.Current.Column);
            return null;
        }

        // Literals
        if (context.Check(TokenType.True))
        {
            context.Advance();
            return new LiteralExpression(true);
        }

        if (context.Check(TokenType.False))
        {
            context.Advance();
            return new LiteralExpression(false);
        }

        if (context.Check(TokenType.Null))
        {
            context.Advance();
            return new LiteralExpression(null);
        }

        if (context.Check(TokenType.NumberLiteral))
        {
            var value = context.Current.Value;
            context.Advance();
            return new LiteralExpression(double.Parse(value));
        }

        if (context.Check(TokenType.StringLiteral))
        {
            var value = context.Current.Value;
            context.Advance();
            return new LiteralExpression(value);
        }

        // Plain identifier or property path (event data)
        if (context.Check(TokenType.Identifier))
        {
            var path = ParsePropertyPath(context);
            return path is not null ? new EventDataExpression(path) : null;
        }

        context.Errors.Add("Expected expression", context.Current.Line, context.Current.Column);
        return null;
    }

    TemplateExpression? ParseTemplate(IParsingContext context, string template)
    {
        var parts = new List<TemplatePart>();
        var i = 0;

        while (i < template.Length)
        {
            var dollarIndex = template.IndexOf("${", i);
            if (dollarIndex == -1)
            {
                // No more interpolations
                if (i < template.Length)
                {
                    parts.Add(new TemplateTextPart(template.Substring(i)));
                }
                break;
            }

            // Add text before interpolation
            if (dollarIndex > i)
            {
                parts.Add(new TemplateTextPart(template.Substring(i, dollarIndex - i)));
            }

            // Find closing brace
            var closeIndex = template.IndexOf('}', dollarIndex + 2);
            if (closeIndex == -1)
            {
                context.Errors.Add("Unterminated template expression", context.Current.Line, context.Current.Column);
                return null;
            }

            // Parse the expression inside ${}
            var exprText = template.Substring(dollarIndex + 2, closeIndex - dollarIndex - 2);
            var expr = ParseTemplateExpression(exprText);
            if (expr is null) return null;
            parts.Add(new TemplateExpressionPart(expr));

            i = closeIndex + 1;
        }

        return new TemplateExpression(parts);
    }

    Expression? ParseTemplateExpression(string exprText)
    {
        // Simple parser for expressions within template
        exprText = exprText.Trim();

        if (exprText.StartsWith("$eventContext."))
        {
            return new EventContextExpression(exprText.Substring(14));
        }

        if (exprText == "$eventSourceId")
        {
            return new EventSourceIdExpression();
        }

        // Treat plain identifiers as event data property paths
        return new EventDataExpression(exprText);
    }

    string? ParsePropertyPath(IParsingContext context)
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

        return string.Join('.', parts);
    }
}
