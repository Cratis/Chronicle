// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents how an event creates or updates a node.
/// </summary>
[ProtoContract]
public class GraphNodeFromDefinition
{
    /// <summary>
    /// Gets or sets the expression resolving the key of the node.
    /// </summary>
    [ProtoMember(1)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the property name to expression, including counter expressions.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public IDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets whether the event only sets properties on a node that already exists.
    /// </summary>
    [ProtoMember(3)]
    public bool UpdateOnly { get; set; }

    /// <summary>
    /// Gets or sets the optional collection property of the event to run the mapping for, once per item. Expressions address the item's properties as <c>collection.property</c>.
    /// </summary>
    [ProtoMember(4)]
    public string? ForEach { get; set; }
}
