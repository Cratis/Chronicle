// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures.Engine.DeclarationLanguage.AST;
using Cratis.Chronicle.Captures.Engine.DeclarationLanguage.Visitors;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.Parsers;

/// <summary>
/// Parses source blocks.
/// </summary>
public class SourceBlockParser
{
    /// <summary>
    /// Parses a source block from the given context.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <returns>The parsed source block, or null if parsing failed.</returns>
    public SourceBlock? Parse(IParsingContext context)
    {
        var sourceToken = context.Expect(TokenType.Source);
        if (sourceToken is null)
        {
            return null;
        }

        if (!TryParseSourceType(context, out var sourceType))
        {
            return null;
        }

        if (context.Expect(TokenType.Indent, "Expected an indented source block") is null)
        {
            return null;
        }

        var properties = new Dictionary<string, string>(StringComparer.Ordinal);
        while (!context.Check(TokenType.Dedent) && !context.IsAtEnd)
        {
            var propertyToken = context.Current;
            if (!IsPropertyToken(propertyToken.Type))
            {
                context.ReportError($"Unexpected token '{propertyToken.Value}' in source block");
                context.Advance();
                continue;
            }

            context.Advance();
            properties[propertyToken.Value] = ParsingHelpers.CollectLineRemainder(context, propertyToken.Line);
        }

        context.Expect(TokenType.Dedent);

        return new SourceBlock(sourceType, properties)
        {
            Line = sourceToken.Line,
            Column = sourceToken.Column
        };
    }

    static bool TryParseSourceType(IParsingContext context, out SourceType sourceType)
    {
        sourceType = SourceType.Api;

        if (context.Check(TokenType.Api))
        {
            context.Advance();
            sourceType = SourceType.Api;
            return true;
        }

        if (context.Check(TokenType.Webhook))
        {
            context.Advance();
            sourceType = SourceType.Webhook;
            return true;
        }

        if (context.Check(TokenType.Message))
        {
            context.Advance();
            sourceType = SourceType.Message;
            return true;
        }

        context.ReportError("Expected source type 'api', 'webhook' or 'message'");
        return false;
    }

    static bool IsPropertyToken(TokenType type) => type is TokenType.Api or TokenType.Poll or TokenType.Auth or TokenType.Route or TokenType.Path or TokenType.Topic;
}
