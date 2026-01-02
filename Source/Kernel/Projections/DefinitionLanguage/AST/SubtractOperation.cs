// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.LanguageDefinition.AST;

/// <summary>
/// Represents a subtract operation.
/// </summary>
/// <param name="PropertyName">The property to subtract from.</param>
/// <param name="Value">The value to subtract.</param>
public record SubtractOperation(string PropertyName, Expression Value) : MappingOperation;
