// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Projections.Graphs.for_GraphProjectionPipeline.when_handling_events;

public class and_events_arrive_out_of_order : given.a_graph_projection
{
    GraphNode? _person;

    async Task Because()
    {
        await _pipeline.Handle(Event(PersonRegistered, 9, "p1", ("personId", "p1"), ("name", "Newer")));
        await _pipeline.Handle(Event(PersonRegistered, 3, "p1", ("personId", "p1"), ("name", "Older")));
        _person = await Person("p1");
    }

    [Fact] void should_keep_the_write_of_the_later_event() => _person!.Properties["name"].ShouldEqual("Newer");
}
