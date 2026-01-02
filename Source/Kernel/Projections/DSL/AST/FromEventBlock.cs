// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DSL.AST;

/// <summary>
/// Represents an event rule block (on EventType).
/// </summary>
/// <param name="EventType">The event type this rule applies to.</param>
/// <param name="AutoMap">Whether to auto map.</param>
/// <param name="Key">Optional key specification.</param>
/// <param name="CompositeKey">Optional composite key specification.</param>
/// <param name="ParentKey">Optional parent key (for child projections).</param>
/// <param name="Mappings">Collection of mapping operations.</param>
public record FromEventBlock(
    TypeRef EventType,
    bool AutoMap,
    Expression? Key,
    CompositeKeyDirective? CompositeKey,
    Expression? ParentKey,
    IReadOnlyList<MappingOperation> Mappings) : ProjectionDirective;
