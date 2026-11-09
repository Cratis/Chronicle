// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_deleting_an_edge.with_a_sequence_number_at_or_below_the_watermark.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_deleting_an_edge;

/// <summary>
/// A delete by an event at or below the watermark of the edge changes nothing.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class with_a_sequence_number_at_or_below_the_watermark(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public long Edges;

        async Task Because()
        {
            await _run.Apply(5, new UpsertEdge(MemberOf("p1", "g1"), Props()));
            await _run.Apply(5, new DeleteEdge(MemberOf("p1", "g1")));
            await _run.Apply(4, new DeleteEdge(MemberOf("p1", "g1")));
            Edges = await CountEdges("MEMBER_OF");
        }
    }

    [Fact] void should_keep_the_edge() => Context.Edges.ShouldEqual(1L);
}
