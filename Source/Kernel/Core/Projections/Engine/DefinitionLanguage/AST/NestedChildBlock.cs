// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.AST;

/// <summary>
/// Represents a nested object block appearing inside a children or nested block.
/// </summary>
/// <param name="PropertyName">The name of the nested object property.</param>
/// <param name="AutoMap">Whether to auto map: Inherit (no directive), Enabled (automap), Disabled (no automap).</param>
/// <param name="ChildBlocks">Collection of child blocks (from, join, nested, clear with, etc.).</param>
public record NestedChildBlock(
    string PropertyName,
    AutoMap AutoMap,
    IReadOnlyList<ChildBlock> ChildBlocks) : ChildBlock;
