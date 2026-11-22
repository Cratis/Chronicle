// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Concepts.Projections.Graphs;

/// <summary>
/// Represents the mappings from events to a node label.
/// </summary>
/// <param name="From">Mappings that create or update nodes, keyed by event type.</param>
/// <param name="RemovedWith">Mappings that delete nodes, keyed by event type.</param>
[Immutable]
[GenerateSerializer]
[Alias("Cratis.Chronicle.Concepts.Projections.Graphs.GraphNodeProjectionDefinition")]
public record GraphNodeProjectionDefinition(
    IReadOnlyDictionary<string, GraphNodeFromDefinition> From,
    IReadOnlyDictionary<string, GraphNodeRemovedWithDefinition> RemovedWith)
{
    /// <summary>
    /// Gets mappings that create or update nodes, keyed by event type.
    /// </summary>
    [Id(0)]
    public IReadOnlyDictionary<string, GraphNodeFromDefinition> From { get; init => field = ImmutableDefinitions.Snapshot(value); } = ImmutableDefinitions.Snapshot(From);

    /// <summary>
    /// Gets mappings that delete nodes, keyed by event type.
    /// </summary>
    [Id(1)]
    public IReadOnlyDictionary<string, GraphNodeRemovedWithDefinition> RemovedWith { get; init => field = ImmutableDefinitions.Snapshot(value); } = ImmutableDefinitions.Snapshot(RemovedWith);
}
