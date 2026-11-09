// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_replaying.and_another_projection_has_edges_to_a_stale_node.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_replaying;

/// <summary>
/// A stale node that the edge of another projection points at is kept as the bare node the edge would have created.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class and_another_projection_has_edges_to_a_stale_node(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public IReadOnlyDictionary<string, object>? Person2;
        public IReadOnlyList<IReadOnlyDictionary<string, object>> Edges = default!;

        async Task Because()
        {
            await _run.Apply(10, new UpsertNode(Person("p2"), Props(("personId", "p2"), ("name", "Stale"))));
            await _run.Apply(OtherOwner, 11, new UpsertEdge(MemberOf("p2", "g1"), Props()));
            var replay = ReplayStartedAt(20);
            await _sink.BeginReplay(Owner, replay);
            await _sink.EndReplay(Owner, replay);
            Person2 = await StoredPerson("p2");
            Edges = await StoredEdges("MEMBER_OF");
        }
    }

    [Fact] void should_keep_the_node_because_an_edge_cannot_exist_without_it() => Context.Person2.ShouldNotBeNull();
    [Fact] void should_make_the_node_bare() => Context.Person2!.Keys.Where(_ => _ != "personId" && _ != "_namespace").ShouldBeEmpty();
    [Fact] void should_keep_the_edge_of_the_other_projection() => Context.Edges.Count.ShouldEqual(1);
}
