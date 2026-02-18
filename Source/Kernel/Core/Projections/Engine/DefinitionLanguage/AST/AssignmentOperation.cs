// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.AST;

/// <summary>
/// Represents a property assignment.
/// </summary>
/// <param name="PropertyName">The target property name.</param>
/// <param name="Value">The value expression.</param>
public record AssignmentOperation(string PropertyName, Expression Value) : MappingOperation;
