// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.AST;

/// <summary>
/// Represents a 'with' event block within a join.
/// </summary>
/// <param name="EventType">The event type.</param>
/// <param name="AutoMap">Whether to auto map: Inherit (no directive), Enabled (automap), Disabled (no automap).</param>
/// <param name="Mappings">Collection of mapping operations.</param>
public record WithEventBlock(
    TypeRef EventType,
    AutoMap AutoMap,
    IReadOnlyList<MappingOperation> Mappings) : AstNode;
