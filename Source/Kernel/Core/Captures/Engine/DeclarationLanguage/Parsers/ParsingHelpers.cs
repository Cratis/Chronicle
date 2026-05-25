// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Captures.Engine.DeclarationLanguage.Visitors;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.Parsers;

/// <summary>
/// Helper methods for line-based parsing tasks.
/// </summary>
static class ParsingHelpers
{
    /// <summary>
    /// Parses a dot-separated property path.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <returns>The parsed property path, or null if parsing failed.</returns>
    public static string? ParsePropertyPath(IParsingContext context)
    {
        if (!context.Check(TokenType.Identifier))
        {
            context.ReportError("Expected identifier");
            return null;
        }

        var parts = new List<string> { context.Current.Value };
        context.Advance();

        while (context.Check(TokenType.Dot))
        {
            context.Advance();
            var token = context.Expect(TokenType.Identifier);
            if (token is null)
            {
                return null;
            }

            parts.Add(token.Value);
        }

        return string.Join('.', parts);
    }

    /// <summary>
    /// Collects and reconstructs the remaining tokens on the specified line.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <param name="line">The line number to collect from.</param>
    /// <returns>The reconstructed text.</returns>
    public static string CollectLineRemainder(IParsingContext context, int line)
    {
        var tokens = new List<Token>();

        while (!context.IsAtEnd &&
               !context.Check(TokenType.Dedent) &&
               !context.Check(TokenType.Indent) &&
               context.Current.Line == line)
        {
            tokens.Add(context.Current);
            context.Advance();
        }

        return Reconstruct(tokens);
    }

    /// <summary>
    /// Collects and reconstructs tokens on the specified line until one of the stop tokens is reached.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <param name="line">The line number to collect from.</param>
    /// <param name="stopTokens">Token types that stop collection.</param>
    /// <returns>The reconstructed text.</returns>
    public static string CollectUntil(IParsingContext context, int line, params TokenType[] stopTokens)
    {
        var tokens = new List<Token>();

        while (!context.IsAtEnd &&
               !context.Check(TokenType.Dedent) &&
               !context.Check(TokenType.Indent) &&
               context.Current.Line == line &&
               !stopTokens.Contains(context.Current.Type))
        {
            tokens.Add(context.Current);
            context.Advance();
        }

        return Reconstruct(tokens);
    }

    /// <summary>
    /// Parses a simple scalar value.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <returns>The parsed value, or null if parsing failed.</returns>
    public static string? ParseSimpleValue(IParsingContext context)
    {
        if (context.IsAtEnd)
        {
            context.ReportError("Expected value");
            return null;
        }

        var token = context.Current;
        context.Advance();

        return token.Type switch
        {
            TokenType.Identifier or TokenType.NumberLiteral or TokenType.NumberRef or TokenType.StringRef => token.Value,
            TokenType.StringLiteral => token.Value,
            TokenType.True => "true",
            TokenType.False => "false",
            TokenType.Null => "null",
            TokenType.Star => "*",
            _ => ReportUnexpectedValue(context, token)
        };
    }

    static string? ReportUnexpectedValue(IParsingContext context, Token token)
    {
        context.ReportError($"Unexpected value '{token.Value}'");

        return null;
    }

    static string Reconstruct(IEnumerable<Token> tokens)
    {
        var builder = new StringBuilder();
        Token? previous = null;

        foreach (var token in tokens)
        {
            var value = token.Type switch
            {
                TokenType.StringLiteral => $"\"{token.Value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"",
                TokenType.TemplateLiteral => $"`{token.Value}`",
                _ => token.Value
            };

            if (builder.Length > 0 && ShouldAddSpace(previous, token))
            {
                builder.Append(' ');
            }

            builder.Append(value);
            previous = token;
        }

        return builder.ToString();
    }

    static bool ShouldAddSpace(Token? previous, Token current)
    {
        if (previous is null)
        {
            return false;
        }

        if (previous.Type == TokenType.Dot || previous.Type == TokenType.Dollar)
        {
            return false;
        }

        if (current.Type == TokenType.Dot)
        {
            return false;
        }

        return true;
    }
}
