// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.AST;

/// <summary>
/// Represents a join block.
/// </summary>
/// <param name="JoinName">The name of the join.</param>
/// <param name="OnProperty">The property to join on.</param>
/// <param name="EventTypes">The event types that populate the join.</param>
/// <param name="AutoMap">Whether to auto map: Inherit (no directive), Enabled (automap), Disabled (no automap).</param>
/// <param name="Mappings">Collection of mapping operations.</param>
public record JoinBlock(
    string JoinName,
    string OnProperty,
    IReadOnlyList<TypeRef> EventTypes,
    AutoMap AutoMap,
    IReadOnlyList<MappingOperation> Mappings) : ProjectionDirective;
