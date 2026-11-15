// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs.for_InMemoryGraphSink.when_replaying;

public class and_an_element_was_not_written_during_the_replay : given.a_graph_sink
{
    GraphNode? _kept;
    GraphNode? _stale;
    GraphEdge? _staleEdge;
    GraphNode? _otherProjection;
    GraphNode? _bare;

    async Task Because()
    {
        await Apply(1, new UpsertNode(Person("kept"), Props(("name", "Old"))), new UpsertNode(Person("stale"), Props(("name", "Gone"))));
        await Apply(2, new UpsertEdge(MemberOf("stale", "g1"), Props()));
        await Apply(OtherOwner, 3, new UpsertNode(Person("theirs"), Props(("name", "Theirs"))));

        var context = ReplayStartedAt(10);
        await _sink.BeginReplay(Owner, context);
        await Apply(1, new UpsertNode(Person("kept"), Props(("name", "New"))));
        await _sink.EndReplay(Owner, context);

        _kept = await NodeOf(Person("kept"));
        _stale = await NodeOf(Person("stale"));
        _staleEdge = await EdgeOf(MemberOf("stale", "g1"));
        _otherProjection = await NodeOf(Person("theirs"));
        _bare = await NodeOf(Group("g1"));
    }

    [Fact] void should_rewrite_the_element_written_in_the_replay() => _kept!.Properties["name"].ShouldEqual("New");
    [Fact] void should_sweep_the_stale_node() => _stale.ShouldBeNull();
    [Fact] void should_sweep_the_stale_edge() => _staleEdge.ShouldBeNull();
    [Fact] void should_not_touch_the_elements_of_another_projection() => _otherProjection.ShouldNotBeNull();
    [Fact] void should_not_delete_a_node_the_projection_did_not_declare() => _bare.ShouldNotBeNull();
}
