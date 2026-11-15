// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Represents a relation of a <see cref="GraphQueryNode"/>: an edge followed in one direction to the node of another view.
/// </summary>
/// <param name="Edge">The edge type.</param>
/// <param name="Direction">The <see cref="GraphViewDirection"/>.</param>
/// <param name="Cardinality">The <see cref="GraphViewCardinality"/>.</param>
/// <param name="IdentifyingProperty">The identifying property of the edge, or null. Related items are ordered by far node key, then by it.</param>
/// <param name="Target">The <see cref="GraphQueryNode"/> of the target view.</param>
public sealed record GraphQueryRelation(
    string Edge,
    GraphViewDirection Direction,
    GraphViewCardinality Cardinality,
    string? IdentifyingProperty,
    GraphQueryNode Target);
