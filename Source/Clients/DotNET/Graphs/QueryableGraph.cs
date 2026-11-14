// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Represents an implementation of <see cref="IGraph"/> that runs queries through an <see cref="IGraphQueryExecutor"/>.
/// </summary>
/// <param name="views">The <see cref="IGraphViews"/> that can be queried.</param>
/// <param name="executor">The <see cref="IGraphQueryExecutor"/> that runs the queries.</param>
public sealed class QueryableGraph(IGraphViews views, IGraphQueryExecutor executor) : IGraph
{
    readonly GraphRowHydrator _hydrator = new(views.NamingPolicy);

    /// <inheritdoc/>
    public IGraphQuery<TView> Nodes<TView>()
        where TView : class => new GraphQueryBuilder<TView>(views, executor, _hydrator);
}
