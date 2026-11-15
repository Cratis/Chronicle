// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Represents an implementation of <see cref="IGraphProjectionBuilder"/> that builds a <see cref="GraphModel"/>.
/// </summary>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> for naming properties.</param>
internal sealed class GraphProjectionBuilder(INamingPolicy namingPolicy) : IGraphProjectionBuilder
{
    readonly List<GraphNodeModel> _nodes = [];
    readonly List<GraphEdgeModel> _edges = [];

    /// <inheritdoc/>
    public IGraphProjectionBuilder Node<TNode>(Action<IGraphNodeBuilder<TNode>> configure)
    {
        var builder = new GraphNodeBuilder<TNode>(namingPolicy);
        configure(builder);
        _nodes.Add(builder.Build());
        return this;
    }

    /// <inheritdoc/>
    public IGraphProjectionBuilder Edge<TEdge, TSource, TTarget>(Action<IGraphEdgeBuilder<TEdge, TSource, TTarget>> configure)
    {
        var builder = new GraphEdgeBuilder<TEdge, TSource, TTarget>(namingPolicy);
        configure(builder);
        _edges.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Build the graph model of a graph projection.
    /// </summary>
    /// <param name="projection">The <see cref="IGraphProjection"/> to define.</param>
    /// <returns>The <see cref="GraphModel"/>.</returns>
    public GraphModel Build(IGraphProjection projection)
    {
        projection.Define(this);
        return new(ModelBoundGraphReader.GraphNameOf(projection.GetType()), _nodes, _edges);
    }
}
