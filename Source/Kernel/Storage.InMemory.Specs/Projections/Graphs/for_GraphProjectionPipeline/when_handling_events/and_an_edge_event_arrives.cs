// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Projections.Graphs.for_GraphProjectionPipeline.when_handling_events;

public class and_an_edge_event_arrives : given.a_graph_projection
{
    GraphEdge? _edge;
    GraphNode? _person;
    GraphNode? _group;

    async Task Because()
    {
        await _pipeline.Handle(Event(MemberAdded, 7, "p1", ("personId", "p1"), ("groupId", "g1"), ("since", "2020")));
        _edge = await MemberOf("p1", "g1");
        _person = await Person("p1");
        _group = await Group("g1");
    }

    [Fact] void should_upsert_the_edge_with_its_properties() => _edge!.Properties["since"].ShouldEqual("2020");
    [Fact] void should_ensure_the_source_node() => _person.ShouldNotBeNull();
    [Fact] void should_ensure_the_target_node() => _group.ShouldNotBeNull();
}
