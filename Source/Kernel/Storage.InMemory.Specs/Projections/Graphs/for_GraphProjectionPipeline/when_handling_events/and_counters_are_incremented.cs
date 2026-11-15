// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Projections.Graphs.for_GraphProjectionPipeline.when_handling_events;

public class and_counters_are_incremented : given.a_graph_projection
{
    GraphNode? _person;
    GraphNode? _group;

    async Task Because()
    {
        await _pipeline.Handle(Event(PersonVisited, 1, "p1", ("personId", "p1")));
        await _pipeline.Handle(Event(PersonVisited, 2, "p1", ("personId", "p1")));
        await _pipeline.Handle(Event(PersonVisited, 2, "p1", ("personId", "p1")));
        await _pipeline.Handle(Event(MemberAdded, 3, "p1", ("personId", "p1"), ("groupId", "g1")));
        await _pipeline.Handle(Event(MemberAdded, 4, "p2", ("personId", "p2"), ("groupId", "g1")));
        _person = await Person("p1");
        _group = await Group("g1");
    }

    [Fact] void should_count_each_event_once_even_when_redelivered() => Convert.ToInt64(_person!.Properties["visits"]).ShouldEqual(2L);
    [Fact] void should_count_on_a_node_that_an_edge_event_also_creates() => Convert.ToInt64(_group!.Properties["members"]).ShouldEqual(2L);
}
