// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.AST;

/// <summary>
/// Represents an event rule within a children block.
/// </summary>
/// <param name="EventType">The event type.</param>
/// <param name="Key">Optional key specification.</param>
/// <param name="CompositeKey">Optional composite key specification.</param>
/// <param name="ParentKey">Optional parent key.</param>
/// <param name="Mappings">Collection of mapping operations.</param>
public record ChildOnEventBlock(
    TypeRef EventType,
    Expression? Key,
    CompositeKeyDirective? CompositeKey,
    Expression? ParentKey,
    IReadOnlyList<MappingOperation> Mappings) : ChildBlock;
