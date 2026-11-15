// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_preparing_an_initial_run.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink;

/// <summary>
/// Preparing an initial run removes everything the owner wrote, and nothing else.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class when_preparing_an_initial_run(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public IReadOnlyDictionary<string, object>? Person1;
        public IReadOnlyDictionary<string, object>? OtherOwnersPerson;
        public IReadOnlyDictionary<string, object>? Group1;
        public long Edges;

        async Task Because()
        {
            await _run.Apply(1, new UpsertNode(Person("p1"), Props(("personId", "p1"))), new UpsertEdge(MemberOf("p1", "g1"), Props()), new UpsertEdge(Visited("p1", "g1", "v1"), Props()));
            await _run.Apply(OtherOwner, 2, new UpsertNode(Person("o1"), Props(("personId", "o1"))));
            await _sink.PrepareInitialRun(Owner);
            Person1 = await StoredPerson("p1");
            OtherOwnersPerson = await StoredPerson("o1");
            Group1 = await StoredGroup("g1");
            Edges = await CountEdges("MEMBER_OF") + await CountEdges("VISITED");
        }
    }

    [Fact] void should_remove_the_nodes_of_the_owner() => Context.Person1.ShouldBeNull();
    [Fact] void should_remove_the_edges_of_the_owner() => Context.Edges.ShouldEqual(0L);
    [Fact] void should_leave_the_nodes_of_another_projection() => Context.OtherOwnersPerson.ShouldNotBeNull();
    [Fact] void should_leave_the_node_no_one_owns() => Context.Group1.ShouldNotBeNull();
}
