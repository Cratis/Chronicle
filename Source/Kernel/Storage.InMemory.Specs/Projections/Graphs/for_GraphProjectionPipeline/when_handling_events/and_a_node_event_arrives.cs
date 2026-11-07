// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Projections.Graphs.for_GraphProjectionPipeline.when_handling_events;

public class and_a_node_event_arrives : given.a_graph_projection
{
    GraphNode? _person;

    async Task Because()
    {
        await _pipeline.Handle(Event(PersonRegistered, 1, "p1", ("personId", "p1"), ("name", "Ada")));
        _person = await Person("p1");
    }

    [Fact] void should_upsert_the_node() => _person.ShouldNotBeNull();
    [Fact] void should_map_the_property() => _person!.Properties["name"].ShouldEqual("Ada");
    [Fact] void should_set_the_key_property() => _person!.Properties["personId"].ShouldEqual("p1");
}
