// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_upserting_an_edge.without_an_identifying_property_twice.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_upserting_an_edge;

/// <summary>
/// An edge without an identifying property is identified by its type and its two ends, so upserting it again updates the one edge.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class without_an_identifying_property_twice(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public IReadOnlyList<IReadOnlyDictionary<string, object>> Edges = default!;
        public GraphEdge Found = default!;

        async Task Because()
        {
            await _run.Apply(1, new UpsertEdge(MemberOf("p1", "g1"), Props(("since", "2020"))));
            await _run.Apply(2, new UpsertEdge(MemberOf("p1", "g1"), Props(("since", "2024"))));
            await _run.Apply(2, new UpsertEdge(MemberOf("p1", "g1"), Props(("since", "Redelivered"))));
            Edges = await StoredEdges("MEMBER_OF");
            Found = (await _sink.FindEdge(Owner, MemberOf("p1", "g1")))!;
        }
    }

    [Fact] void should_identify_the_edge_by_its_type_and_its_ends() => Context.Edges.Count.ShouldEqual(1);
    [Fact] void should_update_the_properties() => Context.Edges[0]["since"].ShouldEqual("2024");
    [Fact] void should_move_the_watermark() => Context.Edges[0]["_watermark"].ShouldEqual(2L);
    [Fact] void should_find_the_edge() => Context.Found.ShouldNotBeNull();
    [Fact] void should_report_the_properties_of_the_edge() => Context.Found.Properties["since"].ShouldEqual("2024");
}
