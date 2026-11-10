// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_upserting_an_edge.with_different_identities.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_upserting_an_edge;

/// <summary>
/// Edges of one type between the same two nodes are told apart by the identifying property of the type.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class with_different_identities(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public IReadOnlyList<IReadOnlyDictionary<string, object>> Edges = default!;
        public GraphEdge First = default!;
        public GraphEdge Second = default!;
        public GraphEdge Unknown = default!;

        async Task Because()
        {
            await _run.Apply(1, new UpsertEdge(Visited("p1", "g1", "v1"), Props(("count", 1))), new UpsertEdge(Visited("p1", "g1", "v2"), Props(("count", 2))));
            Edges = await StoredEdges("VISITED");
            First = (await _sink.FindEdge(Owner, Visited("p1", "g1", "v1")))!;
            Second = (await _sink.FindEdge(Owner, Visited("p1", "g1", "v2")))!;
            Unknown = (await _sink.FindEdge(Owner, Visited("p1", "g1", "v3")))!;
        }
    }

    [Fact] void should_keep_an_edge_for_every_identity() => Context.Edges.Count.ShouldEqual(2);
    [Fact] void should_store_the_identity_under_the_name_of_the_definition() => Context.Edges.Select(_ => _["visitId"]).ShouldContainOnly("v1", "v2");
    [Fact] void should_find_the_first_edge_by_its_identity() => Context.First.Properties["count"].ShouldEqual(1L);
    [Fact] void should_find_the_second_edge_by_its_identity() => Context.Second.Properties["count"].ShouldEqual(2L);
    [Fact] void should_not_find_an_identity_that_was_never_written() => Context.Unknown.ShouldBeNull();
}
