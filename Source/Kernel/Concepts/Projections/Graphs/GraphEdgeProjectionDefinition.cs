// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Concepts.Projections.Graphs;

/// <summary>
/// Represents the mappings from events to an edge type.
/// </summary>
/// <param name="From">Mappings that create or update edges, keyed by event type.</param>
/// <param name="RemovedWith">Mappings that delete edges, keyed by event type.</param>
[Immutable]
[GenerateSerializer]
[Alias("Cratis.Chronicle.Concepts.Projections.Graphs.GraphEdgeProjectionDefinition")]
public record GraphEdgeProjectionDefinition(
    IReadOnlyDictionary<string, GraphEdgeFromDefinition> From,
    IReadOnlyDictionary<string, GraphEdgeRemovedWithDefinition> RemovedWith)
{
    /// <summary>
    /// Gets mappings that create or update edges, keyed by event type.
    /// </summary>
    [Id(0)]
    public IReadOnlyDictionary<string, GraphEdgeFromDefinition> From { get; init => field = ImmutableDefinitions.Snapshot(value); } = ImmutableDefinitions.Snapshot(From);

    /// <summary>
    /// Gets mappings that delete edges, keyed by event type.
    /// </summary>
    [Id(1)]
    public IReadOnlyDictionary<string, GraphEdgeRemovedWithDefinition> RemovedWith { get; init => field = ImmutableDefinitions.Snapshot(value); } = ImmutableDefinitions.Snapshot(RemovedWith);
}
