// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs.for_InMemoryGraphSink.when_replaying;

public class and_a_stale_element_is_rewritten : given.a_graph_sink
{
    GraphNode? _duringReplayBefore;
    GraphNode? _node;

    async Task Because()
    {
        await Apply(50, new UpsertNode(Person("p1"), Props(("name", "Ada"), ("visits", 7))));

        var context = ReplayStartedAt(10);
        await _sink.BeginReplay(Owner, context);
        _duringReplayBefore = await NodeOf(Person("p1"));
        await Apply(1, new UpsertNode(Person("p1"), Props(("visits", 1))));
        await _sink.EndReplay(Owner, context);
        _node = await NodeOf(Person("p1"));
    }

    [Fact] void should_read_as_absent_until_it_is_written_in_the_replay() => _duringReplayBefore.ShouldBeNull();
    [Fact] void should_accept_a_write_below_the_old_watermark() => _node!.Properties["visits"].ShouldEqual(1L);
    [Fact] void should_start_the_element_over() => _node!.Properties.ContainsKey("name").ShouldBeFalse();
}
