// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Defines the entry point for reading property graphs through views.
/// </summary>
/// <remarks>
/// A host package that reaches the graph store provides it, with its own <see cref="IGraphQueryExecutor"/>. In
/// specifications the graph scenario of the testing package provides one over the in-memory graph.
/// </remarks>
public interface IGraph
{
    /// <summary>
    /// Start a query for the roots of a view.
    /// </summary>
    /// <typeparam name="TView">The type of the view.</typeparam>
    /// <returns>The <see cref="IGraphQuery{TView}"/> to add clauses to and run.</returns>
    /// <exception cref="NotAGraphView">Thrown when the type is not a valid view.</exception>
    IGraphQuery<TView> Nodes<TView>()
        where TView : class;
}
