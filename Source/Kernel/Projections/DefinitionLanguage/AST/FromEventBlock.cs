// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.AST;

/// <summary>
/// Represents an event rule block (on EventType).
/// </summary>
/// <param name="EventType">The event type this rule applies to.</param>
/// <param name="AutoMap">Whether to auto map: Inherit (no directive), Enabled (automap), Disabled (no automap).</param>
/// <param name="Key">Optional key specification.</param>
/// <param name="CompositeKey">Optional composite key specification.</param>
/// <param name="ParentKey">Optional parent key (for child projections).</param>
/// <param name="Mappings">Collection of mapping operations.</param>
public record FromEventBlock(
    TypeRef EventType,
    AutoMap AutoMap,
    Expression? Key,
    CompositeKeyDirective? CompositeKey,
    Expression? ParentKey,
    IReadOnlyList<MappingOperation> Mappings) : ProjectionDirective;
