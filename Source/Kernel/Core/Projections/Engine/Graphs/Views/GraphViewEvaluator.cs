// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views;

/// <summary>
/// Evaluates a query over graph views against the nodes and edges an <see cref="IGraphSink"/> holds.
/// </summary>
/// <remarks>
/// <para>
/// This is the kernel's evaluator of the semantics every implementation of graph views shares, held to the shared query
/// vectors by its specifications, as the in-memory executor of the .NET client is. A query selects roots with its clauses and
/// reads each root through the members of its view; finite nesting follows the relations of the views.
/// </para>
/// <para>
/// Roots are distinct by node key and ordered by key. Clauses only select roots: a relation always holds every related item,
/// ordered by the key of the far node and then by edge identity. A relation of cardinality many without related nodes is
/// empty, one without a related node is null, and a bare node an edge created reads with its properties absent.
/// </para>
/// </remarks>
public static class GraphViewEvaluator
{
    /// <summary>
    /// Evaluate a query.
    /// </summary>
    /// <param name="sink">The <see cref="IGraphSink"/> holding the graph.</param>
    /// <param name="graph">The <see cref="GraphDefinition"/> of the graph.</param>
    /// <param name="views">The <see cref="GraphView">views</see> declared over the graph.</param>
    /// <param name="query">The <see cref="GraphViewQuery"/> to evaluate.</param>
    /// <returns>The <see cref="GraphViewRow">rows</see> of the roots that match, in key order; at most one for a single query.</returns>
    /// <exception cref="InvalidGraphViews">Thrown when the views or the query break the rules of graph views.</exception>
    public static async Task<IReadOnlyList<GraphViewRow>> Evaluate(IGraphSink sink, GraphDefinition graph, IReadOnlyList<GraphView> views, GraphViewQuery query)
    {
        var issues = GraphViewValidator.Validate(graph, views).Concat(GraphViewQueryValidator.Validate(views, query)).ToList();
        if (issues.Count > 0) throw new InvalidGraphViews(issues);

        var byName = views.ToDictionary(_ => _.Name, StringComparer.Ordinal);
        var root = byName[query.View];
        var snapshot = await GraphViewSnapshot.Load(sink, graph, byName, root);
        var rows = snapshot.NodesOf(root)
            .Where(node => query.Clauses.All(clause => snapshot.Satisfies(root, node, clause)))
            .Select(node => snapshot.RowOf(root, node, null));
        return query.FirstOnly ? [.. rows.Take(1)] : [.. rows];
    }
}
