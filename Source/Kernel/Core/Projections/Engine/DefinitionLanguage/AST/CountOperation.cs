// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.AST;

/// <summary>
/// Represents a count operation.
/// </summary>
/// <param name="PropertyName">The property to count into.</param>
public record CountOperation(string PropertyName) : MappingOperation;
