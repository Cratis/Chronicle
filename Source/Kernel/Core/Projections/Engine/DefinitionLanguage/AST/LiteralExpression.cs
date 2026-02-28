// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.AST;

/// <summary>
/// Represents a literal value.
/// </summary>
/// <param name="Value">The literal value (string, number, bool, or null).</param>
public record LiteralExpression(object? Value) : Expression;
