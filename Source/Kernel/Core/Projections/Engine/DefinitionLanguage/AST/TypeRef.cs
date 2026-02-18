// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.AST;

/// <summary>
/// Represents a type reference (e.g., EventType, ReadModelType).
/// </summary>
/// <param name="Name">The type name (can be qualified with dots).</param>
public record TypeRef(string Name) : AstNode;
