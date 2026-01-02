// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.Visitors;

/// <summary>
/// Provides context and helper methods for parsing operations.
/// </summary>
public interface IParsingContext
{
    /// <summary>
    /// Gets the current token.
    /// </summary>
    Token Current { get; }

    /// <summary>
    /// Gets whether the parser is at the end of input.
    /// </summary>
    bool IsAtEnd { get; }

    /// <summary>
    /// Gets the errors collection.
    /// </summary>
    ParsingErrors Errors { get; }

    /// <summary>
    /// Peeks at the next token without advancing.
    /// </summary>
    /// <param name="offset">The offset from the current position (default is 1).</param>
    /// <returns>The token at the offset position.</returns>
    Token Peek(int offset = 1);

    /// <summary>
    /// Advances to the next token.
    /// </summary>
    void Advance();

    /// <summary>
    /// Checks if the current token is of the specified type.
    /// </summary>
    /// <param name="type">The token type to check.</param>
    /// <returns>True if the current token matches the type, false otherwise.</returns>
    bool Check(TokenType type);

    /// <summary>
    /// Expects and consumes a token of the specified type.
    /// </summary>
    /// <param name="type">The expected token type.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The token if found, or null if not found (error will be recorded).</returns>
    Token? Expect(TokenType type, string message = "");
}
