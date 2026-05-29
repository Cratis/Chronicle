// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage;

#pragma warning disable CA1027
/// <summary>
/// Represents the different token types in the capture definition language.
/// </summary>
public enum TokenType
#pragma warning restore CA1027
{
    /// <summary>
    /// An identifier.
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
    /// A template string enclosed in backticks.
    /// </summary>
    TemplateLiteral = 3,

    /// <summary>
    /// The keyword <c>capture</c>.
    /// </summary>
    Capture = 4,

    /// <summary>
    /// The keyword <c>source</c>.
    /// </summary>
    Source = 5,

    /// <summary>
    /// The keyword <c>key</c>.
    /// </summary>
    Key = 6,

    /// <summary>
    /// The keyword <c>map</c>.
    /// </summary>
    Map = 7,

    /// <summary>
    /// The keyword <c>append</c>.
    /// </summary>
    Append = 8,

    /// <summary>
    /// The keyword <c>when</c>.
    /// </summary>
    When = 9,

    /// <summary>
    /// The keyword <c>nested</c>.
    /// </summary>
    Nested = 10,

    /// <summary>
    /// The keyword <c>children</c>.
    /// </summary>
    Children = 11,

    /// <summary>
    /// The keyword <c>identified</c>.
    /// </summary>
    Identified = 12,

    /// <summary>
    /// The keyword <c>by</c>.
    /// </summary>
    By = 13,

    /// <summary>
    /// The keyword <c>api</c>.
    /// </summary>
    Api = 14,

    /// <summary>
    /// The keyword <c>webhook</c>.
    /// </summary>
    Webhook = 15,

    /// <summary>
    /// The keyword <c>message</c>.
    /// </summary>
    Message = 16,

    /// <summary>
    /// The keyword <c>poll</c>.
    /// </summary>
    Poll = 17,

    /// <summary>
    /// The keyword <c>auth</c>.
    /// </summary>
    Auth = 18,

    /// <summary>
    /// The keyword <c>route</c>.
    /// </summary>
    Route = 19,

    /// <summary>
    /// The keyword <c>path</c>.
    /// </summary>
    Path = 20,

    /// <summary>
    /// The keyword <c>topic</c>.
    /// </summary>
    Topic = 21,

    /// <summary>
    /// The keyword <c>from</c>.
    /// </summary>
    From = 22,

    /// <summary>
    /// The keyword <c>to</c>.
    /// </summary>
    To = 23,

    /// <summary>
    /// The keyword <c>or</c>.
    /// </summary>
    Or = 24,

    /// <summary>
    /// The keyword <c>and</c>.
    /// </summary>
    And = 25,

    /// <summary>
    /// The keyword <c>added</c>.
    /// </summary>
    Added = 26,

    /// <summary>
    /// The keyword <c>removed</c>.
    /// </summary>
    Removed = 27,

    /// <summary>
    /// The keyword <c>translate</c>.
    /// </summary>
    Translate = 28,

    /// <summary>
    /// The keyword <c>split</c>.
    /// </summary>
    Split = 29,

    /// <summary>
    /// The keyword <c>bearer</c>.
    /// </summary>
    Bearer = 30,

    /// <summary>
    /// The equals operator.
    /// </summary>
    Equals = 31,

    /// <summary>
    /// The arrow operator.
    /// </summary>
    Arrow = 32,

    /// <summary>
    /// The dot operator.
    /// </summary>
    Dot = 33,

    /// <summary>
    /// The dollar sign.
    /// </summary>
    Dollar = 34,

    /// <summary>
    /// The wildcard star.
    /// </summary>
    Star = 35,

    /// <summary>
    /// An indentation increase.
    /// </summary>
    Indent = 36,

    /// <summary>
    /// An indentation decrease.
    /// </summary>
    Dedent = 37,

    /// <summary>
    /// A newline token.
    /// </summary>
    NewLine = 38,

    /// <summary>
    /// The end of input.
    /// </summary>
    EndOfInput = 39,

    /// <summary>
    /// An invalid token.
    /// </summary>
    Invalid = 40,

    /// <summary>
    /// The keyword <see langword="true"/>.
    /// </summary>
    True = 41,

    /// <summary>
    /// The keyword <see langword="false"/>.
    /// </summary>
    False = 42,

    /// <summary>
    /// The keyword <see langword="null"/>.
    /// </summary>
    Null = 43,

    /// <summary>
    /// An unquoted string reference.
    /// </summary>
    StringRef = 44,

    /// <summary>
    /// A numeric reference with suffix.
    /// </summary>
    NumberRef = 45,

    /// <summary>
    /// A comment.
    /// </summary>
    Comment = 46
}
