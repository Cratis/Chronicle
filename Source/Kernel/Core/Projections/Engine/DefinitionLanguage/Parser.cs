// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.DefinitionLanguage.AST;
using Cratis.Chronicle.Projections.Engine.DefinitionLanguage.Parsers;
using Cratis.Chronicle.Projections.Engine.DefinitionLanguage.Visitors;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage;

/// <summary>
/// Parser for the indentation-based projection declaration language that converts tokens into an AST.
/// </summary>
/// <param name="tokens">The tokens to parse.</param>
public class Parser(IEnumerable<Token> tokens) : IParsingContext
{
    readonly List<Token> _tokens = tokens.Where(t => t.Type != TokenType.NewLine).ToList();
    readonly ProjectionParser _projections = new();
    int _position;

    /// <inheritdoc/>
    public Token Current => _position < _tokens.Count ? _tokens[_position] : new Token(TokenType.EndOfInput, string.Empty, 0, 0);

    /// <inheritdoc/>
    public bool IsAtEnd => Current.Type == TokenType.EndOfInput;

    /// <inheritdoc/>
    public ParsingErrors Errors { get; } = new([]);

    /// <summary>
    /// Parses the projection declaration language into a Document AST.
    /// </summary>
    /// <returns>The parsed document or parsing errors.</returns>
    public Result<Document, ParsingErrors> Parse()
    {
        var projections = new List<ProjectionNode>();

        while (!IsAtEnd)
        {
            var projection = _projections.Parse(this);
            if (projection is not null)
            {
                projections.Add(projection);
            }

            // If we encountered errors and couldn't parse, try to recover by advancing
            if (Errors.HasErrors && !IsAtEnd && projection is null)
            {
                Advance();
            }
        }

        return Errors.HasErrors
            ? Errors
            : new Document(projections);
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
            var msg = string.IsNullOrEmpty(message) ? $"Expected {type}" : message;
            ReportError(msg);
            return null;
        }
        var token = Current;
        Advance();
        return token;
    }

    /// <inheritdoc/>
    public void ReportError(string message) => Errors.Add(message, Current.Line, Current.Column);
}
