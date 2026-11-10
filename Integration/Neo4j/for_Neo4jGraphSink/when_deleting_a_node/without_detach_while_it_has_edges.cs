// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_deleting_a_node.without_detach_while_it_has_edges.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_deleting_a_node;

/// <summary>
/// A node that still has edges is not deleted without detach, and since a changeset is one transaction, nothing else of it is written either.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class without_detach_while_it_has_edges(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public Exception? Error;
        public IReadOnlyDictionary<string, object>? Person1;
        public IReadOnlyDictionary<string, object>? Person2;
        public long Edges;

        async Task Because()
        {
            await _run.Apply(1, new UpsertNode(Person("p1"), Props(("personId", "p1"))), new UpsertEdge(MemberOf("p1", "g1"), Props()));
            Error = await Catch.Exception(() => _run.Apply(2, new UpsertNode(Person("p2"), Props(("personId", "p2"))), new DeleteNode(Person("p1"), false)));
            Person1 = await StoredPerson("p1");
            Person2 = await StoredPerson("p2");
            Edges = await CountEdges("MEMBER_OF");
        }
    }

    [Fact] void should_refuse_because_the_node_still_has_edges() => Context.Error.ShouldBeOfExactType<NodeStillHasEdges>();
    [Fact] void should_keep_the_node() => Context.Person1.ShouldNotBeNull();
    [Fact] void should_keep_the_edge() => Context.Edges.ShouldEqual(1L);
    [Fact] void should_roll_back_the_rest_of_the_changeset() => Context.Person2.ShouldBeNull();
}
