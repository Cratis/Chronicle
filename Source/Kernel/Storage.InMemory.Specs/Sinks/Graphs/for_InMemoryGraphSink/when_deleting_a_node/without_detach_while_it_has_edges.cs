// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs.for_InMemoryGraphSink.when_deleting_a_node;

public class without_detach_while_it_has_edges : given.a_graph_sink
{
    Exception? _exception;
    GraphNode? _node;

    async Task Because()
    {
        await Apply(1, new UpsertNode(Person("p1"), Props(("name", "Ada"))), new UpsertEdge(MemberOf("p1", "g1"), Props()));
        _exception = await Catch.Exception(() => Apply(2, new DeleteNode(Person("p1"), false)));
        _node = await NodeOf(Person("p1"));
    }

    [Fact] void should_refuse_the_delete() => _exception.ShouldBeOfExactType<NodeStillHasEdges>();
    [Fact] void should_leave_the_node_in_place() => _node.ShouldNotBeNull();
}
