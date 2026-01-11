// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.AST;

/// <summary>
/// Represents a from $all block in the projection DSL.
/// </summary>
/// <param name="Mappings">The property mappings.</param>
/// <param name="AutoMap">Auto-mapping setting for this block.</param>
public record FromAllBlock(
    IReadOnlyList<MappingOperation> Mappings,
    AutoMap AutoMap = AutoMap.Inherit) : ProjectionDirective;
