// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents how an event deletes a node.
/// </summary>
[ProtoContract]
public class GraphNodeRemovedWithDefinition
{
    /// <summary>
    /// Gets or sets the expression resolving the key of the node.
    /// </summary>
    [ProtoMember(1)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the edges attached to the node are deleted with it.
    /// </summary>
    [ProtoMember(2)]
    public bool Detach { get; set; }

    /// <summary>
    /// Gets or sets the optional collection property of the event to run the mapping for, once per item. Expressions address the item's properties as <c language="csharp">collection.property</c>.
    /// </summary>
    [ProtoMember(3)]
    public string? ForEach { get; set; }
}
