// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Graphs;

/// <summary>
/// Represents the mappings from events to a node label.
/// </summary>
/// <param name="From">Mappings that create or update nodes, keyed by event type.</param>
/// <param name="RemovedWith">Mappings that delete nodes, keyed by event type.</param>
public record GraphNodeProjectionDefinition(
    IDictionary<string, GraphNodeFromDefinition> From,
    IDictionary<string, GraphNodeRemovedWithDefinition> RemovedWith);
