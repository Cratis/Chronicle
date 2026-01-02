// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DefinitionLanguage.AST;

/// <summary>
/// Represents a decrement operation.
/// </summary>
/// <param name="PropertyName">The property to decrement.</param>
public record DecrementOperation(string PropertyName) : MappingOperation;
