// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures.Engine.DeclarationLanguage.AST;
using Cratis.Chronicle.Captures.Engine.DeclarationLanguage.Parsers;
using Cratis.Chronicle.Captures.Engine.DeclarationLanguage.Visitors;
using Cratis.Monads;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage;

/// <summary>
/// Parser for the indentation-based capture declaration language that converts tokens into an AST.
/// </summary>
/// <param name="tokens">The tokens to parse.</param>
public class Parser(IEnumerable<Token> tokens) : IParsingContext
{
    readonly List<Token> _tokens = tokens.Where(_ => _.Type != TokenType.NewLine).ToList();
    readonly CaptureParser _captures = new();
    int _position;

    /// <inheritdoc/>
    public Token Current => _position < _tokens.Count ? _tokens[_position] : new Token(TokenType.EndOfInput, string.Empty, 0, 0);

    /// <inheritdoc/>
    public bool IsAtEnd => Current.Type == TokenType.EndOfInput;

    /// <inheritdoc/>
    public ParsingErrors Errors { get; } = new([]);

    /// <summary>
    /// Parses the capture declaration language into a document AST.
    /// </summary>
    /// <returns>The parsed document or parsing errors.</returns>
    public Result<CaptureDocument, ParsingErrors> Parse()
    {
        var captures = new List<CaptureNode>();

        while (!IsAtEnd)
        {
            var capture = _captures.Parse(this);
            if (capture is not null)
            {
                captures.Add(capture);
            }

            if (Errors.HasErrors && !IsAtEnd && capture is null)
            {
                Advance();
            }
        }

        return Errors.HasErrors
            ? Errors
            : new CaptureDocument(captures);
    }

    /// <inheritdoc/>
    public Token Peek(int offset = 1) => _position + offset < _tokens.Count ? _tokens[_position + offset] : new Token(TokenType.EndOfInput, string.Empty, 0, 0);

    /// <inheritdoc/>
    public void Advance() => _position++;

    /// <inheritdoc/>
    public bool Check(TokenType type) => Current.Type == type;

    /// <inheritdoc/>
    public Token? Expect(TokenType type, string message = "")
    {
        if (!Check(type))
        {
            ReportError(string.IsNullOrEmpty(message) ? $"Expected {type}" : message);
            return null;
        }

        var token = Current;
        Advance();
        return token;
    }

    /// <inheritdoc/>
    public void ReportError(string message) => Errors.Add(message, Current.Line, Current.Column);
}
