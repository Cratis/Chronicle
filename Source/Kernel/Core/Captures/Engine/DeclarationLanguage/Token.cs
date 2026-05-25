// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage;

/// <summary>
/// Represents a token in the capture declaration language.
/// </summary>
/// <param name="Type">The token type.</param>
/// <param name="Value">The raw token value.</param>
/// <param name="Line">The line number.</param>
/// <param name="Column">The column number.</param>
public record Token(TokenType Type, string Value, int Line, int Column);
