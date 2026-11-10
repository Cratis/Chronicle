// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_upserting_an_edge.whose_end_nodes_do_not_exist.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_upserting_an_edge;

/// <summary>
/// Upserting an edge whose end nodes do not exist creates both as bare nodes, then the edge with the bookkeeping.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class whose_end_nodes_do_not_exist(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public IReadOnlyDictionary<string, object> Person1 = default!;
        public IReadOnlyDictionary<string, object> Group1 = default!;
        public GraphNode FoundPerson = default!;
        public IReadOnlyList<IReadOnlyDictionary<string, object>> Edges = default!;

        async Task Because()
        {
            await _run.Apply(3, new UpsertEdge(MemberOf("p1", "g1"), Props(("since", "2020"))));
            Person1 = (await StoredPerson("p1"))!;
            Group1 = (await StoredGroup("g1"))!;
            FoundPerson = (await _sink.FindNode(Owner, Person("p1")))!;
            Edges = await StoredEdges("MEMBER_OF");
        }
    }

    [Fact] void should_create_the_source_node() => Context.Person1["personId"].ShouldEqual("p1");
    [Fact] void should_create_the_target_node() => Context.Group1["groupId"].ShouldEqual("g1");
    [Fact] void should_create_the_end_nodes_bare() => Context.Person1.Keys.Where(_ => _ != "personId" && _ != "_namespace").ShouldBeEmpty();
    [Fact] void should_report_a_bare_node_without_a_watermark() => Context.FoundPerson.Watermark.IsUnavailable.ShouldBeTrue();
    [Fact] void should_create_one_edge() => Context.Edges.Count.ShouldEqual(1);
    [Fact] void should_store_the_properties_of_the_edge() => Context.Edges[0]["since"].ShouldEqual("2020");
    [Fact] void should_stamp_the_watermark_of_the_edge() => Context.Edges[0]["_watermark"].ShouldEqual(3L);
    [Fact] void should_stamp_the_owner_of_the_edge() => Context.Edges[0]["_owner"].ShouldEqual("owner");
    [Fact] void should_stamp_the_generation_of_the_edge() => Context.Edges[0]["_generation"].ShouldEqual(0L);
    [Fact] void should_stamp_the_namespace_of_the_edge_only_when_namespaces_share_the_database() => Context.Edges[0].ContainsKey("_namespace").ShouldEqual(Context.IsSingleDatabase);
}
