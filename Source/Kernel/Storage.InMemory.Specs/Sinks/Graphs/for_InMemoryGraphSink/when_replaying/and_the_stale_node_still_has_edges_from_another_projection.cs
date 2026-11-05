// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs.for_InMemoryGraphSink.when_replaying;

public class and_the_stale_node_still_has_edges_from_another_projection : given.a_graph_sink
{
    GraphNode? _node;
    GraphEdge? _edge;

    async Task Because()
    {
        await Apply(1, new UpsertNode(Person("p1"), Props(("name", "Ada"))));
        await Apply(OtherOwner, 2, new UpsertEdge(MemberOf("p1", "g1"), Props()));

        var context = ReplayStartedAt(10);
        await _sink.BeginReplay(Owner, context);
        await _sink.EndReplay(Owner, context);
        _node = await NodeOf(Person("p1"));
        _edge = await EdgeOf(MemberOf("p1", "g1"));
    }

    [Fact] void should_keep_the_node_as_a_bare_node() => _node!.Properties.ShouldBeEmpty();
    [Fact] void should_keep_the_edge_of_the_other_projection() => _edge.ShouldNotBeNull();
}
