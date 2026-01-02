// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DSL.AST;

/// <summary>
/// Represents an add operation.
/// </summary>
/// <param name="PropertyName">The property to add to.</param>
/// <param name="Value">The value to add.</param>
public record AddOperation(string PropertyName, Expression Value) : MappingOperation;
