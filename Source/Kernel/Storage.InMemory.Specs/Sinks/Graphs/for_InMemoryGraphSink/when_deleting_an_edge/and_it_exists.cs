// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs.for_InMemoryGraphSink.when_deleting_an_edge;

public class and_it_exists : given.a_graph_sink
{
    GraphEdge? _edge;
    GraphNode? _source;

    async Task Because()
    {
        await Apply(3, new UpsertEdge(MemberOf("p1", "g1"), Props()));
        await Apply(2, new DeleteEdge(MemberOf("p1", "g1")));
        var survivedLateDelete = await EdgeOf(MemberOf("p1", "g1"));
        await Apply(4, new DeleteEdge(MemberOf("p1", "g1")));
        _edge = await EdgeOf(MemberOf("p1", "g1"));
        _source = await NodeOf(Person("p1"));
        _survivedLateDelete = survivedLateDelete is not null;
    }

    bool _survivedLateDelete;

    [Fact] void should_ignore_a_delete_below_the_watermark() => _survivedLateDelete.ShouldBeTrue();
    [Fact] void should_delete_the_edge() => _edge.ShouldBeNull();
    [Fact] void should_leave_the_end_nodes_in_place() => _source.ShouldNotBeNull();
}
