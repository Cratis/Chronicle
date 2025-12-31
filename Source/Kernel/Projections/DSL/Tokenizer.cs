// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Projections.DSL;

/// <summary>
/// Tokenizer for the projection DSL.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Tokenizer"/> class.
/// </remarks>
/// <param name="input">The input string to tokenize.</param>
public class Tokenizer(string input)
{
    static readonly Dictionary<string, TokenType> _keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        { "key", TokenType.Key },
        { "increment", TokenType.Increment },
        { "decrement", TokenType.Decrement },
        { "count", TokenType.Count },
        { "by", TokenType.By },
        { "on", TokenType.On },
        { "join", TokenType.Join },
        { "identified", TokenType.Identified },
        { "removedWith", TokenType.RemovedWith }
    };

    readonly string _input = input;
    int _position;
    int _line = 1;
    int _column = 1;

    /// <summary>
    /// Tokenizes the input string into a list of tokens.
    /// </summary>
    /// <returns>A list of tokens.</returns>
    public IEnumerable<Token> Tokenize()
    {
        var tokens = new List<Token>();

        while (_position < _input.Length)
        {
            var token = NextToken();
            if (token.Type != TokenType.NewLine)
            {
                tokens.Add(token);
            }
        }

        tokens.Add(new Token(TokenType.EndOfInput, string.Empty, _line, _column));
        return tokens;
    }

    Token NextToken()
    {
        SkipWhitespace();

        if (_position >= _input.Length)
        {
            return new Token(TokenType.EndOfInput, string.Empty, _line, _column);
        }

        var currentChar = _input[_position];
        var tokenLine = _line;
        var tokenColumn = _column;

        if (currentChar == '\n')
        {
            _position++;
            _line++;
            _column = 1;
            return new Token(TokenType.NewLine, "\n", tokenLine, tokenColumn);
        }

        if (currentChar == '|')
        {
            _position++;
            _column++;
            return new Token(TokenType.Pipe, "|", tokenLine, tokenColumn);
        }

        if (currentChar == '=')
        {
            _position++;
            _column++;
            return new Token(TokenType.Equals, "=", tokenLine, tokenColumn);
        }

        if (currentChar == '+')
        {
            // support '+=' compound operator
            if (_position + 1 < _input.Length && _input[_position + 1] == '=')
            {
                _position += 2;
                _column += 2;
                return new Token(TokenType.PlusEquals, "+=", tokenLine, tokenColumn);
            }

            _position++;
            _column++;
            return new Token(TokenType.Plus, "+", tokenLine, tokenColumn);
        }

        if (currentChar == '-')
        {
            // support '-=' compound operator
            if (_position + 1 < _input.Length && _input[_position + 1] == '=')
            {
                _position += 2;
                _column += 2;
                return new Token(TokenType.MinusEquals, "-=", tokenLine, tokenColumn);
            }

            _position++;
            _column++;
            return new Token(TokenType.Minus, "-", tokenLine, tokenColumn);
        }

        if (currentChar == '.')
        {
            _position++;
            _column++;
            return new Token(TokenType.Dot, ".", tokenLine, tokenColumn);
        }

        if (currentChar == ':')
        {
            _position++;
            _column++;
            return new Token(TokenType.Colon, ":", tokenLine, tokenColumn);
        }

        if (currentChar == ',')
        {
            _position++;
            _column++;
            return new Token(TokenType.Comma, ",", tokenLine, tokenColumn);
        }

        if (currentChar == '[')
        {
            _position++;
            _column++;
            return new Token(TokenType.LeftBracket, "[", tokenLine, tokenColumn);
        }

        if (currentChar == ']')
        {
            _position++;
            _column++;
            return new Token(TokenType.RightBracket, "]", tokenLine, tokenColumn);
        }

        if (currentChar == '"' || currentChar == '\'')
        {
            return ReadStringLiteral(tokenLine, tokenColumn);
        }

        if (char.IsDigit(currentChar))
        {
            return ReadNumber(tokenLine, tokenColumn);
        }

        if (char.IsLetter(currentChar) || currentChar == '_' || currentChar == '$')
        {
            return ReadIdentifierOrKeyword(tokenLine, tokenColumn);
        }

        _position++;
        _column++;
        return new Token(TokenType.Invalid, currentChar.ToString(), tokenLine, tokenColumn);
    }

    Token ReadIdentifierOrKeyword(int tokenLine, int tokenColumn)
    {
        var sb = new StringBuilder();

        while (_position < _input.Length)
        {
            var currentChar = _input[_position];
            if (char.IsLetterOrDigit(currentChar) || currentChar == '_' || currentChar == '$')
            {
                sb.Append(currentChar);
                _position++;
                _column++;
            }
            else
            {
                break;
            }
        }

        var value = sb.ToString();
        var tokenType = _keywords.TryGetValue(value, out var keyword) ? keyword : TokenType.Identifier;

        return new Token(tokenType, value, tokenLine, tokenColumn);
    }

    Token ReadStringLiteral(int tokenLine, int tokenColumn)
    {
        var quoteChar = _input[_position];
        _position++;
        _column++;

        var sb = new StringBuilder();
        while (_position < _input.Length)
        {
            var currentChar = _input[_position];
            if (currentChar == quoteChar)
            {
                _position++;
                _column++;
                break;
            }

            if (currentChar == '\\' && _position + 1 < _input.Length)
            {
                _position++;
                _column++;
                var nextChar = _input[_position];
                sb.Append(nextChar switch
                {
                    'n' => '\n',
                    't' => '\t',
                    'r' => '\r',
                    '\\' => '\\',
                    '"' => '"',
                    '\'' => '\'',
                    _ => nextChar
                });
                _position++;
                _column++;
            }
            else
            {
                sb.Append(currentChar);
                _position++;
                _column++;
            }
        }

        return new Token(TokenType.StringLiteral, sb.ToString(), tokenLine, tokenColumn);
    }

    Token ReadNumber(int tokenLine, int tokenColumn)
    {
        var sb = new StringBuilder();
        var hasDecimalPoint = false;

        while (_position < _input.Length)
        {
            var currentChar = _input[_position];
            if (char.IsDigit(currentChar))
            {
                sb.Append(currentChar);
                _position++;
                _column++;
            }
            else if (currentChar == '.' && !hasDecimalPoint)
            {
                hasDecimalPoint = true;
                sb.Append(currentChar);
                _position++;
                _column++;
            }
            else
            {
                break;
            }
        }

        return new Token(TokenType.NumberLiteral, sb.ToString(), tokenLine, tokenColumn);
    }

    void SkipWhitespace()
    {
        while (_position < _input.Length)
        {
            var currentChar = _input[_position];
            if (currentChar == ' ' || currentChar == '\t' || currentChar == '\r')
            {
                _position++;
                _column++;
            }
            else if (currentChar == '#')
            {
                while (_position < _input.Length && _input[_position] != '\n')
                {
                    _position++;
                    _column++;
                }
            }
            else
            {
                break;
            }
        }
    }
}
