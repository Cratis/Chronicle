// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.AST;

/// <summary>
/// Represents an "every" block within a children block.
/// </summary>
/// <param name="Mappings">Collection of mapping operations.</param>
/// <param name="AutoMap">Whether to auto map: Inherit (no directive), Enabled (automap), Disabled (no automap).</param>
public record ChildEveryBlock(IReadOnlyList<MappingOperation> Mappings, AutoMap AutoMap) : ChildBlock;
