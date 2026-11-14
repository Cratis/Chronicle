// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs;

namespace Cratis.Chronicle.Testing.Graphs;

/// <summary>
/// Represents an implementation of <see cref="IGraphQueryExecutor"/> that evaluates queries against the in-memory graphs
/// of a <see cref="GraphScenario"/>: the reference evaluator for the graph query semantics.
/// </summary>
/// <param name="graphFor">Finds the running graph with a name.</param>
internal sealed class InMemoryGraphQueryExecutor(Func<string, RunningGraph?> graphFor) : IGraphQueryExecutor
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<GraphRow>> Execute(GraphQuery query, CancellationToken cancellationToken = default)
    {
        var graph = graphFor(query.Graph) ?? throw new NoGraphWithName(query.Graph);
        var snapshot = await InMemoryGraphSnapshot.Load(graph, query.Root);
        var roots = snapshot.NodesOf(query.Root)
            .Where(node => query.Clauses.All(clause => snapshot.Satisfies(query.Root, node, clause)))
            .Select(node => snapshot.RowOf(query.Root, node, null));
        return query.Shape == GraphQueryShape.First ? [.. roots.Take(1)] : [.. roots];
    }
}
