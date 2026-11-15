// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_upserting_an_edge.with_the_same_identity_twice.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_upserting_an_edge;

/// <summary>
/// Upserting an edge with an identity that exists updates that edge.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class with_the_same_identity_twice(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public IReadOnlyList<IReadOnlyDictionary<string, object>> Edges = default!;

        async Task Because()
        {
            await _run.Apply(1, new UpsertEdge(Visited("p1", "g1", "v1"), Props(("count", 1))));
            await _run.Apply(2, new UpsertEdge(Visited("p1", "g1", "v1"), Props(("count", 5))));
            Edges = await StoredEdges("VISITED");
        }
    }

    [Fact] void should_keep_one_edge() => Context.Edges.Count.ShouldEqual(1);
    [Fact] void should_update_the_properties() => Context.Edges[0]["count"].ShouldEqual(5L);
}
