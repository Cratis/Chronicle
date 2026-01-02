// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.LanguageDefinition.AST;

/// <summary>
/// Represents a remove rule (removedWith).
/// </summary>
/// <param name="EventType">The event type that triggers removal.</param>
/// <param name="Key">Optional key specification.</param>
/// <param name="ParentKey">Optional parent key.</param>
public record RemoveBlock(TypeRef EventType, Expression? Key, Expression? ParentKey) : ChildBlock;
