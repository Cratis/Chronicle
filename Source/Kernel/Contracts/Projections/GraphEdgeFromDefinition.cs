// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents how an event creates or updates an edge.
/// </summary>
[ProtoContract]
public class GraphEdgeFromDefinition
{
    /// <summary>
    /// Gets or sets the expression resolving the key of the source node.
    /// </summary>
    [ProtoMember(1)]
    public string SourceKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expression resolving the key of the target node.
    /// </summary>
    [ProtoMember(2)]
    public string TargetKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional expression resolving the identity of the edge, for edge types with an identifying property.
    /// </summary>
    [ProtoMember(3)]
    public string? Identity { get; set; }

    /// <summary>
    /// Gets or sets the property name to expression, including counter expressions.
    /// </summary>
    [ProtoMember(4, IsRequired = true)]
    public IDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the optional collection property of the event to run the mapping for, once per item. Expressions address the item's properties as <c language="csharp">collection.property</c>.
    /// </summary>
    [ProtoMember(5)]
    public string? ForEach { get; set; }
}
