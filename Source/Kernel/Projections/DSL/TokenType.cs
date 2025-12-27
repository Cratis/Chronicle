// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DSL;

/// <summary>
/// Represents the different types of tokens in the projection DSL.
/// </summary>
public enum TokenType
{
    /// <summary>
    /// An identifier (e.g., property name, event type name, read model name).
    /// </summary>
    Identifier = 0,

    /// <summary>
    /// The pipe operator (|).
    /// </summary>
    Pipe = 1,

    /// <summary>
    /// The equals operator (=).
    /// </summary>
    Equals = 2,

    /// <summary>
    /// The plus operator (+).
    /// </summary>
    Plus = 3,

    /// <summary>
    /// The minus operator (-).
    /// </summary>
    Minus = 4,

    /// <summary>
    /// The dot operator (.).
    /// </summary>
    Dot = 5,

    /// <summary>
    /// The colon operator (:).
    /// </summary>
    Colon = 6,

    /// <summary>
    /// The comma operator (,).
    /// </summary>
    Comma = 7,

    /// <summary>
    /// The left bracket ([).
    /// </summary>
    LeftBracket = 8,

    /// <summary>
    /// The right bracket (]).
    /// </summary>
    RightBracket = 9,

    /// <summary>
    /// A string literal enclosed in quotes.
    /// </summary>
    StringLiteral = 10,

    /// <summary>
    /// A number literal.
    /// </summary>
    NumberLiteral = 11,

    /// <summary>
    /// The keyword 'key'.
    /// </summary>
    Key = 12,

    /// <summary>
    /// The keyword 'increment'.
    /// </summary>
    Increment = 13,

    /// <summary>
    /// The keyword 'decrement'.
    /// </summary>
    Decrement = 14,

    /// <summary>
    /// The keyword 'count'.
    /// </summary>
    Count = 15,

    /// <summary>
    /// The keyword 'by'.
    /// </summary>
    By = 16,

    /// <summary>
    /// The keyword 'on'.
    /// </summary>
    On = 17,

    /// <summary>
    /// The keyword 'join'.
    /// </summary>
    Join = 18,

    /// <summary>
    /// The keyword 'identified'.
    /// </summary>
    Identified = 19,

    /// <summary>
    /// The keyword 'removedWith'.
    /// </summary>
    RemovedWith = 20,

    /// <summary>
    /// End of input.
    /// </summary>
    EndOfInput = 21,

    /// <summary>
    /// New line character.
    /// </summary>
    NewLine = 22,

    /// <summary>
    /// Invalid token.
    /// </summary>
    Invalid = 23
}
