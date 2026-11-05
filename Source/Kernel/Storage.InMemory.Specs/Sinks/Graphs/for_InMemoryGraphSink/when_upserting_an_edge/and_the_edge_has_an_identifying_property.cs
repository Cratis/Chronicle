// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs.for_InMemoryGraphSink.when_upserting_an_edge;

public class and_the_edge_has_an_identifying_property : given.a_graph_sink
{
    GraphElements<GraphEdge> _edges;
    Exception? _exception;

    async Task Because()
    {
        await Apply(1, new UpsertEdge(Visited("p1", "g1", "a"), Props(("count", 1))));
        await Apply(2, new UpsertEdge(Visited("p1", "g1", "b"), Props(("count", 1))));
        await Apply(3, new UpsertEdge(Visited("p1", "g1", "a"), Props(("count", 2))));
        _exception = await Catch.Exception(() => Apply(4, new UpsertEdge(new("VISITED", Person("p1"), Group("g1")), Props())));
        _edges = await _sink.GetEdges("VISITED");
    }

    [Fact] void should_keep_one_edge_per_identity() => _edges.TotalCount.ShouldEqual(2);
    [Fact] void should_update_the_edge_with_the_same_identity() => _edges.Elements[0].Properties["count"].ShouldEqual(2L);
    [Fact] void should_require_the_identity() => _exception.ShouldBeOfExactType<InvalidOperationException>();
}
