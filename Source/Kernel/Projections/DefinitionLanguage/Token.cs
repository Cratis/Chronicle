// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.LanguageDefinition;

/// <summary>
/// Represents a token in the projection DSL.
/// </summary>
/// <param name="Type">The type of the token.</param>
/// <param name="Value">The value of the token.</param>
/// <param name="Line">The line number where the token appears.</param>
/// <param name="Column">The column number where the token appears.</param>
public record Token(TokenType Type, string Value, int Line, int Column);
