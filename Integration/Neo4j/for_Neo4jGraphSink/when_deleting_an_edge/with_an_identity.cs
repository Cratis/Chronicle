// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_deleting_an_edge.with_an_identity.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_deleting_an_edge;

/// <summary>
/// Deleting an identified edge deletes the edge with that identity, and not the others between the same nodes.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class with_an_identity(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public IReadOnlyList<IReadOnlyDictionary<string, object>> Edges = default!;

        async Task Because()
        {
            await _run.Apply(1, new UpsertEdge(Visited("p1", "g1", "v1"), Props()), new UpsertEdge(Visited("p1", "g1", "v2"), Props()));
            await _run.Apply(2, new DeleteEdge(Visited("p1", "g1", "v1")));
            Edges = await StoredEdges("VISITED");
        }
    }

    [Fact] void should_delete_only_the_edge_with_the_identity() => Context.Edges.Count.ShouldEqual(1);
    [Fact] void should_keep_the_other_identity() => Context.Edges[0]["visitId"].ShouldEqual("v2");
}
