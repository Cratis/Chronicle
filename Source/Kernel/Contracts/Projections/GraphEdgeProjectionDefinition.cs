// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the mappings from events to an edge type.
/// </summary>
[ProtoContract]
public class GraphEdgeProjectionDefinition
{
    /// <summary>
    /// Gets or sets the mappings that create or update edges, keyed by event type in the form id+generation.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public IDictionary<string, GraphEdgeFromDefinition> From { get; set; } = new Dictionary<string, GraphEdgeFromDefinition>();

    /// <summary>
    /// Gets or sets the mappings that delete edges, keyed by event type in the form id+generation.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public IDictionary<string, GraphEdgeRemovedWithDefinition> RemovedWith { get; set; } = new Dictionary<string, GraphEdgeRemovedWithDefinition>();
}
