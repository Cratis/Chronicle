// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Storage.InMemory.Projections.Graphs.for_GraphProjectionPipeline.when_replaying;

public class and_the_projection_is_replayed : given.a_graph_projection
{
    GraphNode? _kept;
    GraphNode? _removedByHistory;
    GraphNode? _group;

    async Task Because()
    {
        await _pipeline.Handle(Event(PersonRegistered, 1, "p1", ("personId", "p1"), ("name", "Ada")));
        await _pipeline.Handle(Event(PersonVisited, 2, "p1", ("personId", "p1")));
        await _pipeline.Handle(Event(PersonRegistered, 3, "p2", ("personId", "p2"), ("name", "Bob")));
        await _pipeline.Handle(Event(MemberAdded, 4, "p1", ("personId", "p1"), ("groupId", "g1")));

        var context = new ReplayContext(new("organization", 1), "container", "revert", DateTimeOffset.UtcNow);
        await _pipeline.BeginReplay(context);
        await _pipeline.Handle(Event(PersonRegistered, 1, "p1", ("personId", "p1"), ("name", "Ada")));
        await _pipeline.Handle(Event(PersonVisited, 2, "p1", ("personId", "p1")));
        await _pipeline.Handle(Event(MemberAdded, 4, "p1", ("personId", "p1"), ("groupId", "g1")));
        await _pipeline.EndReplay(context);

        _kept = await Person("p1");
        _removedByHistory = await Person("p2");
        _group = await Group("g1");
    }

    [Fact] void should_rebuild_counters_from_scratch_instead_of_adding_to_the_old_result() => Convert.ToInt64(_kept!.Properties["visits"]).ShouldEqual(1L);
    [Fact] void should_remove_what_the_replay_did_not_produce() => _removedByHistory.ShouldBeNull();
    [Fact] void should_rebuild_the_graph() => _group.ShouldNotBeNull();
}
