// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DefinitionLanguage.AST;

/// <summary>
/// Represents a remove via join rule.
/// </summary>
/// <param name="EventType">The event type that triggers removal.</param>
/// <param name="Key">Optional key specification.</param>
public record RemoveViaJoinBlock(TypeRef EventType, Expression? Key) : ChildBlock;
