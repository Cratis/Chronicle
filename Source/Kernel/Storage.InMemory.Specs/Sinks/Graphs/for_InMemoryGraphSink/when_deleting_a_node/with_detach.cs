// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs.for_InMemoryGraphSink.when_deleting_a_node;

public class with_detach : given.a_graph_sink
{
    GraphNode? _node;
    GraphNode? _group;
    GraphEdge? _edge;

    async Task Because()
    {
        await Apply(1, new UpsertNode(Person("p1"), Props(("name", "Ada"))), new UpsertEdge(MemberOf("p1", "g1"), Props()));
        await Apply(2, new DeleteNode(Person("p1"), true));
        _node = await NodeOf(Person("p1"));
        _group = await NodeOf(Group("g1"));
        _edge = await EdgeOf(MemberOf("p1", "g1"));
    }

    [Fact] void should_delete_the_node() => _node.ShouldBeNull();
    [Fact] void should_delete_the_attached_edge() => _edge.ShouldBeNull();
    [Fact] void should_leave_the_other_end_in_place() => _group.ShouldNotBeNull();
}
