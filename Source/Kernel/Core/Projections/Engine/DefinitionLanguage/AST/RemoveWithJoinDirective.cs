// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.AST;

/// <summary>
/// Represents a projection-level remove via join directive.
/// </summary>
/// <param name="EventType">The event type that triggers removal.</param>
/// <param name="Key">Optional key specification.</param>
public record RemoveWithJoinDirective(TypeRef EventType, Expression? Key) : ProjectionDirective;
