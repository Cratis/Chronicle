// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the mappings from events to a node label.
/// </summary>
[ProtoContract]
public class GraphNodeProjectionDefinition
{
    /// <summary>
    /// Gets or sets the mappings that create or update nodes, keyed by event type in the form id+generation.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public IDictionary<string, GraphNodeFromDefinition> From { get; set; } = new Dictionary<string, GraphNodeFromDefinition>();

    /// <summary>
    /// Gets or sets the mappings that delete nodes, keyed by event type in the form id+generation.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public IDictionary<string, GraphNodeRemovedWithDefinition> RemovedWith { get; set; } = new Dictionary<string, GraphNodeRemovedWithDefinition>();
}
