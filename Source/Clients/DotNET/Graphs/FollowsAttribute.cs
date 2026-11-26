// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Makes a member of a view a relation: it follows an edge from the node of the view to another view. A collection
/// member holds every related item, a single member the one related item or null.
/// </summary>
/// <typeparam name="TEdge">The type of the edge to follow.</typeparam>
/// <remarks>
/// The direction is derived from the root of the view: outgoing when the root is the source of the edge, incoming
/// when it is the target. An edge whose source and target are the same node type needs an explicit direction.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class FollowsAttribute<TEdge> : Attribute, IFollowsAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FollowsAttribute{TEdge}"/> class, deriving the direction.
    /// </summary>
    public FollowsAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FollowsAttribute{TEdge}"/> class.
    /// </summary>
    /// <param name="direction">The <see cref="GraphViewDirection"/> to follow the edge in.</param>
    public FollowsAttribute(GraphViewDirection direction) => Direction = direction;

    /// <inheritdoc/>
    public Type EdgeType => typeof(TEdge);

    /// <inheritdoc/>
    public GraphViewDirection? Direction { get; }
}
