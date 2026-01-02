// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DefinitionLanguage;

/// <summary>
/// Represents the different types of tokens in the projection DSL.
/// </summary>
public enum TokenType
{
    /// <summary>
    /// An identifier (e.g., property name, event type name).
    /// </summary>
    Identifier = 0,

    /// <summary>
    /// A number literal.
    /// </summary>
    NumberLiteral = 1,

    /// <summary>
    /// A string literal enclosed in quotes.
    /// </summary>
    StringLiteral = 2,

    /// <summary>
    /// A template string with interpolations (`text ${expr}`).
    /// </summary>
    TemplateLiteral = 3,

    /// <summary>
    /// The keyword 'projection'.
    /// </summary>
    Projection = 4,

    /// <summary>
    /// The keyword 'every'.
    /// </summary>
    Every = 5,

    /// <summary>
    /// The keyword 'from'.
    /// </summary>
    From = 6,

    /// <summary>
    /// The keyword 'automap'.
    /// </summary>
    AutoMap = 7,

    /// <summary>
    /// The keyword 'key'.
    /// </summary>
    Key = 8,

    /// <summary>
    /// The keyword 'parent'.
    /// </summary>
    Parent = 9,

    /// <summary>
    /// The keyword 'join'.
    /// </summary>
    Join = 10,

    /// <summary>
    /// The keyword 'events'.
    /// </summary>
    Events = 11,

    /// <summary>
    /// The keyword 'children'.
    /// </summary>
    Children = 12,

    /// <summary>
    /// The keyword 'id'.
    /// </summary>
    Id = 13,

    /// <summary>
    /// The keyword 'remove'.
    /// </summary>
    Remove = 14,

    /// <summary>
    /// The keyword 'via'.
    /// </summary>
    Via = 15,

    /// <summary>
    /// The keyword 'exclude'.
    /// </summary>
    Exclude = 16,

    /// <summary>
    /// The keyword 'increment'.
    /// </summary>
    Increment = 17,

    /// <summary>
    /// The keyword 'decrement'.
    /// </summary>
    Decrement = 18,

    /// <summary>
    /// The keyword 'count'.
    /// </summary>
    Count = 19,

    /// <summary>
    /// The keyword 'add'.
    /// </summary>
    Add = 20,

    /// <summary>
    /// The keyword 'subtract'.
    /// </summary>
    Subtract = 21,

    /// <summary>
    /// The keyword 'by'.
    /// </summary>
    By = 22,

    /// <summary>
    /// The keyword 'true'.
    /// </summary>
    True = 23,

    /// <summary>
    /// The keyword 'false'.
    /// </summary>
    False = 24,

    /// <summary>
    /// The keyword 'null'.
    /// </summary>
    Null = 25,

    /// <summary>
    /// The keyword 'e' (event reference).
    /// </summary>
    EventRef = 26,

    /// <summary>
    /// The keyword 'ctx' (context reference).
    /// </summary>
    ContextRef = 27,

    /// <summary>
    /// The equals operator (=).
    /// </summary>
    Equals = 28,

    /// <summary>
    /// The arrow operator (=>).
    /// </summary>
    Arrow = 29,

    /// <summary>
    /// The dot operator (.).
    /// </summary>
    Dot = 30,

    /// <summary>
    /// The comma operator (,).
    /// </summary>
    Comma = 31,

    /// <summary>
    /// The left brace ({).
    /// </summary>
    LeftBrace = 32,

    /// <summary>
    /// The right brace (}).
    /// </summary>
    RightBrace = 33,

    /// <summary>
    /// The dollar sign ($).
    /// </summary>
    Dollar = 34,

    /// <summary>
    /// Indentation increase.
    /// </summary>
    Indent = 35,

    /// <summary>
    /// Indentation decrease.
    /// </summary>
    Dedent = 36,

    /// <summary>
    /// New line character.
    /// </summary>
    NewLine = 37,

    /// <summary>
    /// End of input.
    /// </summary>
    EndOfInput = 38,

    /// <summary>
    /// Invalid token.
    /// </summary>
    Invalid = 39
}
