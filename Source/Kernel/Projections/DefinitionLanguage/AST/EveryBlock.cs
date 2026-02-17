// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.AST;

/// <summary>
/// Represents a "fromEvery" block.
/// </summary>
/// <param name="Mappings">Collection of mapping operations.</param>
/// <param name="ExcludeChildren">Whether to exclude child projections.</param>
/// <param name="AutoMap">Whether to auto map: Inherit (no directive), Enabled (automap), Disabled (no automap).</param>
public record EveryBlock(IReadOnlyList<MappingOperation> Mappings, bool ExcludeChildren, AutoMap AutoMap) : ProjectionDirective;
