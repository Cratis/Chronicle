// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_setting_properties.on_an_edge.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_setting_properties;

/// <summary>
/// Setting properties on an edge sets them on that edge, found by its ends and its identity.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class on_an_edge(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public IReadOnlyDictionary<string, object> Plain = default!;
        public IReadOnlyDictionary<string, object> Identified = default!;

        async Task Because()
        {
            await _run.Apply(1, new UpsertEdge(MemberOf("p1", "g1"), Props(("since", "2020"))), new UpsertEdge(Visited("p1", "g1", "v1"), Props(("count", 1))));
            await _run.Apply(2, new SetProperties(MemberOf("p1", "g1"), Props(("since", "2024"))), new SetProperties(Visited("p1", "g1", "v1"), Props(("count", 2))));
            Plain = (await StoredEdges("MEMBER_OF"))[0];
            Identified = (await StoredEdges("VISITED"))[0];
        }
    }

    [Fact] void should_set_the_property_of_an_edge_without_an_identity() => Context.Plain["since"].ShouldEqual("2024");
    [Fact] void should_set_the_property_of_an_identified_edge() => Context.Identified["count"].ShouldEqual(2L);
    [Fact] void should_keep_the_identity_of_the_identified_edge() => Context.Identified["visitId"].ShouldEqual("v1");
    [Fact] void should_move_the_watermark_of_the_edge() => Context.Plain["_watermark"].ShouldEqual(2L);
}
