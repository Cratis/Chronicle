// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.AST;

/// <summary>
/// Represents a 'clear with' directive that nulls a nested object when the given event occurs.
/// </summary>
/// <param name="EventType">The event type that triggers clearing the nested object.</param>
public record ClearWithDirective(TypeRef EventType) : ChildBlock;
