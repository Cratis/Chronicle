// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs.for_InMemoryGraphSink.when_upserting_an_edge;

public class and_the_edge_has_no_identifying_property : given.a_graph_sink
{
    GraphElements<GraphEdge> _edges;

    async Task Because()
    {
        await Apply(1, new UpsertEdge(MemberOf("p1", "g1"), Props(("since", "2020"))));
        await Apply(2, new UpsertEdge(MemberOf("p1", "g1"), Props(("since", "2021"))));
        await Apply(2, new UpsertEdge(MemberOf("p1", "g1"), Props(("since", "2022"))));
        _edges = await _sink.GetEdges("MEMBER_OF");
    }

    [Fact] void should_keep_one_edge_between_the_pair() => _edges.TotalCount.ShouldEqual(1);
    [Fact] void should_apply_the_first_write_at_a_new_sequence_number_only() => _edges.Elements[0].Properties["since"].ShouldEqual("2021");
}
