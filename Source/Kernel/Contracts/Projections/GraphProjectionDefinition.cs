// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the graph mappings of a projection that targets a graph.
/// </summary>
[ProtoContract]
public class GraphProjectionDefinition
{
    /// <summary>
    /// Gets or sets the mappings per node label.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public IDictionary<string, GraphNodeProjectionDefinition> Nodes { get; set; } = new Dictionary<string, GraphNodeProjectionDefinition>();

    /// <summary>
    /// Gets or sets the mappings per edge type.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public IDictionary<string, GraphEdgeProjectionDefinition> Edges { get; set; } = new Dictionary<string, GraphEdgeProjectionDefinition>();
}
