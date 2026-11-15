// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Marks a type as a view over a property graph, anchored at a node. A view only reads: its members are properties of
/// the node, relations along edges to other views, or properties of the edge that was followed.
/// </summary>
/// <typeparam name="TNode">The type of the node the view is anchored at. The view belongs to the graph of the node.</typeparam>
/// <remarks>
/// A member is a property of the node, matched by name after the naming policy, unless it has <see cref="FollowsAttribute{TEdge}"/>,
/// which makes it a relation, or <see cref="FromEdgeAttribute"/>, which makes it a property of the edge that was followed.
/// A member with the key attribute, or a member named <c language="csharp">Id</c> when the node has no property of that name, is the key of the node.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class ViewOfAttribute<TNode> : Attribute, IGraphViewAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ViewOfAttribute{TNode}"/> class, naming the view after its type.
    /// </summary>
    public ViewOfAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewOfAttribute{TNode}"/> class.
    /// </summary>
    /// <param name="name">The name of the view, unique in its graph.</param>
    public ViewOfAttribute(string name) => Name = name;

    /// <inheritdoc/>
    public Type NodeType => typeof(TNode);

    /// <inheritdoc/>
    public string? Name { get; }
}
