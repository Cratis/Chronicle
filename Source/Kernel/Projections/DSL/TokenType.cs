// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DSL;

/// <summary>
/// Represents the different types of tokens in the projection DSL.
/// </summary>
public enum TokenType
{
    /// <summary>
    /// An identifier (e.g., property name, event type name).
    /// </summary>
    Identifier,

    /// <summary>
    /// A number literal.
    /// </summary>
    NumberLiteral,

    /// <summary>
    /// A string literal enclosed in quotes.
    /// </summary>
    StringLiteral,

    /// <summary>
    /// A template string with interpolations (`text ${expr}`).
    /// </summary>
    TemplateLiteral,

    /// <summary>
    /// The keyword 'projection'.
    /// </summary>
    Projection,

    /// <summary>
    /// The keyword 'every'.
    /// </summary>
    Every,

    /// <summary>
    /// The keyword 'from'.
    /// </summary>
    From,

    /// <summary>
    /// The keyword 'automap'.
    /// </summary>
    Automap,

    /// <summary>
    /// The keyword 'key'.
    /// </summary>
    Key,

    /// <summary>
    /// The keyword 'parent'.
    /// </summary>
    Parent,

    /// <summary>
    /// The keyword 'join'.
    /// </summary>
    Join,

    /// <summary>
    /// The keyword 'events'.
    /// </summary>
    Events,

    /// <summary>
    /// The keyword 'children'.
    /// </summary>
    Children,

    /// <summary>
    /// The keyword 'id'.
    /// </summary>
    Id,

    /// <summary>
    /// The keyword 'remove'.
    /// </summary>
    Remove,

    /// <summary>
    /// The keyword 'via'.
    /// </summary>
    Via,

    /// <summary>
    /// The keyword 'exclude'.
    /// </summary>
    Exclude,

    /// <summary>
    /// The keyword 'increment'.
    /// </summary>
    Increment,

    /// <summary>
    /// The keyword 'decrement'.
    /// </summary>
    Decrement,

    /// <summary>
    /// The keyword 'count'.
    /// </summary>
    Count,

    /// <summary>
    /// The keyword 'add'.
    /// </summary>
    Add,

    /// <summary>
    /// The keyword 'subtract'.
    /// </summary>
    Subtract,

    /// <summary>
    /// The keyword 'by'.
    /// </summary>
    By,

    /// <summary>
    /// The keyword 'true'.
    /// </summary>
    True,

    /// <summary>
    /// The keyword 'false'.
    /// </summary>
    False,

    /// <summary>
    /// The keyword 'null'.
    /// </summary>
    Null,

    /// <summary>
    /// The keyword 'e' (event reference).
    /// </summary>
    EventRef,

    /// <summary>
    /// The keyword 'ctx' (context reference).
    /// </summary>
    ContextRef,

    /// <summary>
    /// The equals operator (=).
    /// </summary>
    Equals,

    /// <summary>
    /// The arrow operator (=>).
    /// </summary>
    Arrow,

    /// <summary>
    /// The dot operator (.).
    /// </summary>
    Dot,

    /// <summary>
    /// The comma operator (,).
    /// </summary>
    Comma,

    /// <summary>
    /// The left brace ({).
    /// </summary>
    LeftBrace,

    /// <summary>
    /// The right brace (}).
    /// </summary>
    RightBrace,

    /// <summary>
    /// The dollar sign ($).
    /// </summary>
    Dollar,

    /// <summary>
    /// Indentation increase.
    /// </summary>
    Indent,

    /// <summary>
    /// Indentation decrease.
    /// </summary>
    Dedent,

    /// <summary>
    /// New line character.
    /// </summary>
    NewLine,

    /// <summary>
    /// End of input.
    /// </summary>
    EndOfInput,

    /// <summary>
    /// Invalid token.
    /// </summary>
    Invalid,

    // Legacy token types (for backward compatibility with old DSL parser)
    /// <summary>
    /// The pipe operator (|) - Legacy.
    /// </summary>
    [Obsolete("Legacy token type for old pipe-based DSL")]
    Pipe,

    /// <summary>
    /// The colon operator (:) - Legacy.
    /// </summary>
    [Obsolete("Legacy token type for old pipe-based DSL")]
    Colon,

    /// <summary>
    /// The left bracket ([) - Legacy.
    /// </summary>
    [Obsolete("Legacy token type for old pipe-based DSL")]
    LeftBracket,

    /// <summary>
    /// The right bracket (]) - Legacy.
    /// </summary>
    [Obsolete("Legacy token type for old pipe-based DSL")]
    RightBracket,

    /// <summary>
    /// The plus operator (+) - Legacy.
    /// </summary>
    [Obsolete("Legacy token type for old pipe-based DSL")]
    Plus,

    /// <summary>
    /// The minus operator (-) - Legacy.
    /// </summary>
    [Obsolete("Legacy token type for old pipe-based DSL")]
    Minus,

    /// <summary>
    /// The plus-and-equals operator (+=) - Legacy.
    /// </summary>
    [Obsolete("Legacy token type for old pipe-based DSL")]
    PlusEquals,

    /// <summary>
    /// The minus-and-equals operator (-=) - Legacy.
    /// </summary>
    [Obsolete("Legacy token type for old pipe-based DSL")]
    MinusEquals,

    /// <summary>
    /// The keyword 'removedWith' - Legacy.
    /// </summary>
    [Obsolete("Legacy token type for old pipe-based DSL")]
    RemovedWith,

    /// <summary>
    /// The keyword 'identifier' - Legacy.
    /// </summary>
    [Obsolete("Legacy token type for old pipe-based DSL")]
    IdentifierKeyword
}
