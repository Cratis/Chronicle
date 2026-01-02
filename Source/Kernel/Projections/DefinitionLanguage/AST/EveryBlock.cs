// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DSL.AST;

/// <summary>
/// Represents a "fromEvery" block.
/// </summary>
/// <param name="Mappings">Collection of mapping operations.</param>
/// <param name="ExcludeChildren">Whether to exclude child projections.</param>
/// <param name="AutoMap">Whether to auto map.</param>
public record EveryBlock(IReadOnlyList<MappingOperation> Mappings, bool ExcludeChildren, bool AutoMap) : ProjectionDirective;
