// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DefinitionLanguage;

/// <summary>
/// Represents the different types of tokens in the projection DSL.
/// </summary>
#pragma warning disable CA1027 // Mark enums with FlagsAttribute
public enum TokenType
#pragma warning restore CA1027 // Mark enums with FlagsAttribute
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
    /// The keyword 'on'.
    /// </summary>
    On = 10,

    /// <summary>
    /// The keyword 'with'.
    /// </summary>
    With = 11,

    /// <summary>
    /// The keyword 'join'.
    /// </summary>
    Join = 12,

    /// <summary>
    /// The keyword 'events'.
    /// </summary>
    Events = 13,

    /// <summary>
    /// The keyword 'children'.
    /// </summary>
    Children = 14,

    /// <summary>
    /// The keyword 'id'.
    /// </summary>
    Id = 15,

    /// <summary>
    /// The keyword 'remove'.
    /// </summary>
    Remove = 16,

    /// <summary>
    /// The keyword 'via'.
    /// </summary>
    Via = 17,

    /// <summary>
    /// The keyword 'exclude'.
    /// </summary>
    Exclude = 18,

    /// <summary>
    /// The keyword 'increment'.
    /// </summary>
    Increment = 19,

    /// <summary>
    /// The keyword 'decrement'.
    /// </summary>
    Decrement = 20,

    /// <summary>
    /// The keyword 'count'.
    /// </summary>
    Count = 21,

    /// <summary>
    /// The keyword 'sequence'.
    /// </summary>
    Sequence = 22,

    /// <summary>
    /// The keyword 'add'.
    /// </summary>
    Add = 23,

    /// <summary>
    /// The keyword 'subtract'.
    /// </summary>
    Subtract = 24,

    /// <summary>
    /// The keyword 'by'.
    /// </summary>
    By = 25,

    /// <summary>
    /// The keyword 'true'.
    /// </summary>
    True = 26,

    /// <summary>
    /// The keyword 'false'.
    /// </summary>
    False = 27,

    /// <summary>
    /// The keyword 'null'.
    /// </summary>
    Null = 28,

    /// <summary>
    /// The keyword 'e' (event reference).
    /// </summary>
    EventRef = 29,

    /// <summary>
    /// The keyword 'ctx' (context reference).
    /// </summary>
    ContextRef = 30,

    /// <summary>
    /// The equals operator (=).
    /// </summary>
    Equals = 31,

    /// <summary>
    /// The arrow operator (=>).
    /// </summary>
    Arrow = 32,

    /// <summary>
    /// The dot operator (.).
    /// </summary>
    Dot = 33,

    /// <summary>
    /// The comma operator (,).
    /// </summary>
    Comma = 34,

    /// <summary>
    /// The left brace ({).
    /// </summary>
    LeftBrace = 35,

    /// <summary>
    /// The right brace (}).
    /// </summary>
    RightBrace = 36,

    /// <summary>
    /// The dollar sign ($).
    /// </summary>
    Dollar = 37,

    /// <summary>
    /// Indentation increase.
    /// </summary>
    Indent = 38,

    /// <summary>
    /// Indentation decrease.
    /// </summary>
    Dedent = 39,

    /// <summary>
    /// New line character.
    /// </summary>
    NewLine = 40,

    /// <summary>
    /// End of input.
    /// </summary>
    EndOfInput = 41,

    /// <summary>
    /// Invalid token.
    /// </summary>
    Invalid = 42,

    /// <summary>
    /// The keyword 'no'.
    /// </summary>
    No = 45,

    /// <summary>
    /// The keyword 'identified'.
    /// </summary>
    Identified = 46,

    /// <summary>
    /// A comment starting with #.
    /// </summary>
    Comment = 47
}
