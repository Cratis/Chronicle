// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs.for_InMemoryGraphSink.when_upserting_a_node;

public class and_the_event_is_redelivered : given.a_graph_sink
{
    GraphNode? _node;

    async Task Because()
    {
        await Apply(5, new UpsertNode(Person("p1"), Props(("visits", 1))));
        await Apply(5, new UpsertNode(Person("p1"), Props(("visits", 2))));
        _node = await NodeOf(Person("p1"));
    }

    [Fact] void should_skip_the_second_write() => _node!.Properties["visits"].ShouldEqual(1L);
}
