// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Projections.Graphs.for_GraphProjectionPipeline.when_handling_events;

public class and_removal_events_arrive : given.a_graph_projection
{
    GraphEdge? _removedEdge;
    GraphNode? _group;
    GraphNode? _removedPerson;
    GraphEdge? _detachedEdge;

    async Task Because()
    {
        await _pipeline.Handle(Event(PersonRegistered, 1, "p1", ("personId", "p1"), ("name", "Ada")));
        await _pipeline.Handle(Event(PersonRegistered, 2, "p2", ("personId", "p2"), ("name", "Bob")));
        await _pipeline.Handle(Event(MemberAdded, 3, "p1", ("personId", "p1"), ("groupId", "g1"), ("since", "2020")));
        await _pipeline.Handle(Event(MemberAdded, 4, "p2", ("personId", "p2"), ("groupId", "g1"), ("since", "2021")));
        await _pipeline.Handle(Event(MemberRemoved, 5, "p1", ("personId", "p1"), ("groupId", "g1")));
        await _pipeline.Handle(Event(PersonRemoved, 6, "p2", ("personId", "p2")));
        _removedEdge = await MemberOf("p1", "g1");
        _group = await Group("g1");
        _removedPerson = await Person("p2");
        _detachedEdge = await MemberOf("p2", "g1");
    }

    [Fact] void should_delete_the_edge() => _removedEdge.ShouldBeNull();
    [Fact] void should_keep_the_node_the_projection_did_not_remove() => _group.ShouldNotBeNull();
    [Fact] void should_delete_the_node() => _removedPerson.ShouldBeNull();
    [Fact] void should_detach_the_edges_of_the_deleted_node() => _detachedEdge.ShouldBeNull();
}
