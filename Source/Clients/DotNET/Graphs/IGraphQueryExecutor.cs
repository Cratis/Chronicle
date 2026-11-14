// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Defines a system that runs a <see cref="GraphQuery"/> against a graph store. A host package that reaches the
/// graph store implements it, for example a Neo4j executor that translates the query to Cypher.
/// </summary>
/// <remarks>
/// <para>
/// An executor must give exactly the results the shared query semantics define: roots distinct by node key and ordered by
/// key ascending (strings ordinal, numbers numeric); clauses only select roots and never narrow a relation; related items
/// ordered by far node key, then by the identity of an identified edge, with one item per edge; a relation of
/// cardinality many with no related node gives an empty list, one gives null; a property that is absent, as on a bare
/// node an edge created, gives null. For <see cref="GraphQueryShape.First"/> it returns at most the first root.
/// </para>
/// <para>
/// Reads are eventually consistent with the event log and may see stale elements while a projection replays.
/// </para>
/// </remarks>
public interface IGraphQueryExecutor
{
    /// <summary>
    /// Run a query.
    /// </summary>
    /// <param name="query">The <see cref="GraphQuery"/> to run.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the read.</param>
    /// <returns>The <see cref="GraphRow">rows</see>, in result order.</returns>
    Task<IReadOnlyList<GraphRow>> Execute(GraphQuery query, CancellationToken cancellationToken = default);
}
