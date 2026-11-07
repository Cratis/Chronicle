// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Graphs;

/// <summary>
/// Represents the mappings from events to an edge type.
/// </summary>
/// <param name="From">Mappings that create or update edges, keyed by event type.</param>
/// <param name="RemovedWith">Mappings that delete edges, keyed by event type.</param>
public record GraphEdgeProjectionDefinition(
    IDictionary<string, GraphEdgeFromDefinition> From,
    IDictionary<string, GraphEdgeRemovedWithDefinition> RemovedWith);
