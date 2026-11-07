// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs.for_InMemoryGraphSink.when_replaying;

public class and_the_replay_is_left_without_ending : given.a_graph_sink
{
    GraphNode? _node;

    async Task Because()
    {
        await Apply(1, new UpsertNode(Person("p1"), Props(("name", "Ada"))));
        await _sink.BeginReplay(Owner, ReplayStartedAt(10));
        await _sink.LeaveReplay(Owner);
        _node = await NodeOf(Person("p1"));
    }

    [Fact] void should_not_sweep() => _node.ShouldNotBeNull();
}
