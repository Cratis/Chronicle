// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_upserting_a_node.that_an_edge_created.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_upserting_a_node;

/// <summary>
/// A node that only an edge created is bare, and the first upsert of the node claims it, even at the sequence number of the edge.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class that_an_edge_created(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public IReadOnlyDictionary<string, object> BareBefore = default!;
        public IReadOnlyDictionary<string, object> Stored = default!;
        public long Nodes;

        async Task Because()
        {
            await _run.Apply(1, new UpsertEdge(MemberOf("p1", "g1"), Props()));
            BareBefore = (await StoredPerson("p1"))!;
            await _run.Apply(1, new UpsertNode(Person("p1"), Props(("personId", "p1"), ("name", "Ada"))));
            Stored = (await StoredPerson("p1"))!;
            Nodes = await CountNodes("Person");
        }
    }

    [Fact] void should_have_no_watermark_while_only_the_edge_created_it() => Context.BareBefore.ContainsKey("_watermark").ShouldBeFalse();
    [Fact] void should_have_no_owner_while_only_the_edge_created_it() => Context.BareBefore.ContainsKey("_owner").ShouldBeFalse();
    [Fact] void should_write_the_node_at_the_sequence_number_of_the_edge() => Context.Stored["name"].ShouldEqual("Ada");
    [Fact] void should_give_the_node_a_watermark() => Context.Stored["_watermark"].ShouldEqual(1L);
    [Fact] void should_give_the_node_its_owner() => Context.Stored["_owner"].ShouldEqual("owner");
    [Fact] void should_not_create_a_second_node() => Context.Nodes.ShouldEqual(1L);
}
