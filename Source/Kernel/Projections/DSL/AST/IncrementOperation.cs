// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DSL.AST;

/// <summary>
/// Represents an increment operation.
/// </summary>
/// <param name="PropertyName">The property to increment.</param>
public record IncrementOperation(string PropertyName) : MappingOperation;
