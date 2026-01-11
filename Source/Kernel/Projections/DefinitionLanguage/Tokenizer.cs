// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.DefinitionLanguage;

/// <summary>
/// Tokenizer for the indentation-based projection DSL.
/// </summary>
public class Tokenizer
{
    readonly string _input;
    readonly Stack<int> _indentStack = new();
    readonly Queue<Token> _pendingDedents = new();
    readonly ParsingErrors _errors = new([]);
    int _position;
    int _line = 1;
    int _column = 1;
    bool _atLineStart = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="Tokenizer"/> class.
    /// </summary>
    /// <param name="input">The input string to tokenize.</param>
    public Tokenizer(string input)
    {
        _input = input;
        _indentStack.Push(0);
    }

    /// <summary>
    /// Tokenizes the input string into a list of tokens.
    /// </summary>
    /// <returns>A result containing either the list of tokens or parsing errors.</returns>
    public Result<IEnumerable<Token>, ParsingErrors> Tokenize()
    {
        var tokens = new List<Token>();

        while (_position < _input.Length)
        {
            var token = NextToken();
            if (token.Type != TokenType.Invalid)
            {
                tokens.Add(token);
            }
        }

        // Emit remaining dedents at end of file
        while (_indentStack.Count > 1)
        {
            _indentStack.Pop();
            tokens.Add(new Token(TokenType.Dedent, string.Empty, _line, _column));
        }

        tokens.Add(new Token(TokenType.EndOfInput, string.Empty, _line, _column));

        return _errors.HasErrors
            ? _errors
            : tokens;
    }

    Token NextToken()
    {
        // Return pending dedents first
        if (_pendingDedents.Count > 0)
        {
            return _pendingDedents.Dequeue();
        }

        // Handle indentation at line start
        if (_atLineStart)
        {
            return HandleIndentation();
        }

        // Skip whitespace (not at line start)
        while (_position < _input.Length && char.IsWhiteSpace(_input[_position]) && _input[_position] != '\n')
        {
            Advance();
        }

        if (_position >= _input.Length)
        {
            return new Token(TokenType.EndOfInput, string.Empty, _line, _column);
        }

        var currentChar = _input[_position];
        var line = _line;
        var column = _column;

        // Comments
        if (currentChar == '#')
        {
            SkipComment();
            return NextToken();
        }

        // Newline
        if (currentChar == '\n')
        {
            Advance();
            _atLineStart = true;
            return new Token(TokenType.NewLine, "\n", line, column);
        }

        // Template string
        if (currentChar == '`')
        {
            return ScanTemplateString();
        }

        // String literal
        if (currentChar == '"' || currentChar == '\'')
        {
            return ScanString(currentChar);
        }

        // Number
        if (char.IsDigit(currentChar))
        {
            return ScanNumber();
        }

        // Operators and punctuation
        switch (currentChar)
        {
            case '=':
                Advance();
                if (_position < _input.Length && _input[_position] == '>')
                {
                    Advance();
                    return new Token(TokenType.Arrow, "=>", line, column);
                }
                return new Token(TokenType.Equals, "=", line, column);

            case '.':
                Advance();
                return new Token(TokenType.Dot, ".", line, column);

            case ',':
                Advance();
                return new Token(TokenType.Comma, ",", line, column);

            case '{':
                Advance();
                return new Token(TokenType.LeftBrace, "{", line, column);

            case '}':
                Advance();
                return new Token(TokenType.RightBrace, "}", line, column);

            case '$':
                Advance();
                return new Token(TokenType.Dollar, "$", line, column);

            case '@':
                Advance();
                return ScanEscapedIdentifier(line, column);
        }

        // Identifier or keyword
        if (char.IsLetter(currentChar) || currentChar == '_')
        {
            return ScanIdentifierOrKeyword();
        }

        // Unknown character
        Advance();
        return new Token(TokenType.Invalid, currentChar.ToString(), line, column);
    }

    Token HandleIndentation()
    {
        var indentLevel = 0;
        var line = _line;
        var column = _column;

        // Count leading spaces
        while (_position < _input.Length && _input[_position] == ' ')
        {
            indentLevel++;
            Advance();
        }

        // Empty line or comment - ignore indentation
        if (_position >= _input.Length || _input[_position] == '\n' || _input[_position] == '#')
        {
            _atLineStart = false;
            return NextToken();
        }

        _atLineStart = false;
        var currentIndent = _indentStack.Peek();

        if (indentLevel > currentIndent)
        {
            _indentStack.Push(indentLevel);
            return new Token(TokenType.Indent, string.Empty, line, column);
        }

        if (indentLevel < currentIndent)
        {
            // Emit one dedent token for each indentation level we're closing
            var dedentsNeeded = 0;
            while (_indentStack.Count > 1 && _indentStack.Peek() > indentLevel)
            {
                _indentStack.Pop();
                dedentsNeeded++;
            }

            if (_indentStack.Peek() != indentLevel)
            {
                _errors.Add($"Indentation error: {indentLevel} spaces does not match any outer indentation level", line, column);

                // Reset to closest valid indentation to allow parsing to continue
                while (_indentStack.Count > 1 && _indentStack.Peek() > indentLevel)
                {
                    _indentStack.Pop();
                }
            }

            // Queue all but the first dedent
            for (var i = 1; i < dedentsNeeded; i++)
            {
                _pendingDedents.Enqueue(new Token(TokenType.Dedent, string.Empty, line, column));
            }

            return new Token(TokenType.Dedent, string.Empty, line, column);
        }

        // Same indentation - continue normally
        return NextToken();
    }

    Token ScanIdentifierOrKeyword()
    {
        var line = _line;
        var column = _column;
        var sb = new StringBuilder();

        while (_position < _input.Length &&
               (char.IsLetterOrDigit(_input[_position]) || _input[_position] == '_'))
        {
            sb.Append(_input[_position]);
            Advance();
        }

        var value = sb.ToString();

        // Check if this is a keyword (case-insensitive check first)
        var lowerValue = value.ToLowerInvariant();
        if (Keywords.TokenMapping.TryGetValue(lowerValue, out var tokenType))
        {
            // If the value doesn't match the exact lowercase keyword, it's an error
            if (value != lowerValue)
            {
                _errors.Add($"Keyword '{lowerValue}' must be lowercase, not '{value}'", line, column);
            }
            return new Token(tokenType, lowerValue, line, column);
        }

        return new Token(TokenType.Identifier, value, line, column);
    }

    Token ScanString(char quote)
    {
        var line = _line;
        var column = _column;
        var sb = new StringBuilder();
        Advance(); // Skip opening quote

        while (_position < _input.Length && _input[_position] != quote)
        {
            if (_input[_position] == '\\' && _position + 1 < _input.Length)
            {
                Advance();
                var escapeChar = _input[_position];
                sb.Append(escapeChar switch
                {
                    'n' => '\n',
                    't' => '\t',
                    'r' => '\r',
                    '\\' => '\\',
                    '"' => '"',
                    '\'' => '\'',
                    _ => escapeChar
                });
                Advance();
            }
            else
            {
                sb.Append(_input[_position]);
                Advance();
            }
        }

        if (_position >= _input.Length)
        {
            _errors.Add("Unterminated string literal", line, column);
            return new Token(TokenType.StringLiteral, sb.ToString(), line, column);
        }

        Advance(); // Skip closing quote
        return new Token(TokenType.StringLiteral, sb.ToString(), line, column);
    }

    Token ScanTemplateString()
    {
        var line = _line;
        var column = _column;
        var sb = new StringBuilder();
        Advance(); // Skip opening backtick

        while (_position < _input.Length && _input[_position] != '`')
        {
            sb.Append(_input[_position]);
            Advance();
        }

        if (_position >= _input.Length)
        {
            _errors.Add("Unterminated template string", line, column);
            return new Token(TokenType.TemplateLiteral, sb.ToString(), line, column);
        }

        Advance(); // Skip closing backtick
        return new Token(TokenType.TemplateLiteral, sb.ToString(), line, column);
    }

    Token ScanNumber()
    {
        var line = _line;
        var column = _column;
        var sb = new StringBuilder();

        while (_position < _input.Length && (char.IsDigit(_input[_position]) || _input[_position] == '.'))
        {
            sb.Append(_input[_position]);
            Advance();
        }

        return new Token(TokenType.NumberLiteral, sb.ToString(), line, column);
    }

    Token ScanEscapedIdentifier(int line, int column)
    {
        var sb = new StringBuilder();

        // @ was already consumed, now scan the identifier
        while (_position < _input.Length &&
               (char.IsLetterOrDigit(_input[_position]) || _input[_position] == '_'))
        {
            sb.Append(_input[_position]);
            Advance();
        }

        var value = sb.ToString();

        // Escaped identifiers are always returned as identifiers, even if they match keywords
        return new Token(TokenType.Identifier, value, line, column);
    }

    void SkipComment()
    {
        while (_position < _input.Length && _input[_position] != '\n')
        {
            Advance();
        }
    }

    void Advance()
    {
        if (_position < _input.Length)
        {
            if (_input[_position] == '\n')
            {
                _line++;
                _column = 1;
            }
            else
            {
                _column++;
            }
            _position++;
        }
    }
}
