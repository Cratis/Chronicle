// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs.for_InMemoryGraphSink.when_upserting_a_node;

public class and_the_node_was_created_by_an_edge : given.a_graph_sink
{
    GraphNode? _node;

    async Task Because()
    {
        await Apply(9, new UpsertEdge(MemberOf("p1", "g1"), Props()));
        await Apply(4, new UpsertNode(Person("p1"), Props(("name", "Ada"))));
        _node = await NodeOf(Person("p1"));
    }

    [Fact] void should_complete_the_node_even_though_the_event_is_older() => _node!.Properties["name"].ShouldEqual("Ada");
}
