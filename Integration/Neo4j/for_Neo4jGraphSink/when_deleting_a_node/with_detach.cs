// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_deleting_a_node.with_detach.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_deleting_a_node;

/// <summary>
/// Deleting a node with detach deletes the edges with it, and leaves the nodes at their other ends.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class with_detach(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public IReadOnlyDictionary<string, object>? Person1;
        public IReadOnlyDictionary<string, object>? Group1;
        public long Edges;

        async Task Because()
        {
            await _run.Apply(1, new UpsertNode(Person("p1"), Props(("personId", "p1"))), new UpsertEdge(MemberOf("p1", "g1"), Props()));
            await _run.Apply(2, new DeleteNode(Person("p1"), true));
            Person1 = await StoredPerson("p1");
            Group1 = await StoredGroup("g1");
            Edges = await CountEdges("MEMBER_OF");
        }
    }

    [Fact] void should_delete_the_node() => Context.Person1.ShouldBeNull();
    [Fact] void should_delete_the_edges_of_the_node() => Context.Edges.ShouldEqual(0L);
    [Fact] void should_keep_the_node_at_the_other_end() => Context.Group1.ShouldNotBeNull();
}
