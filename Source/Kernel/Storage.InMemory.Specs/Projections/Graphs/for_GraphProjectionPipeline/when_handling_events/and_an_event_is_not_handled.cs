// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.Projections.Graphs.for_GraphProjectionPipeline.when_handling_events;

public class and_an_event_is_not_handled : given.a_graph_projection
{
    bool _hasNodes;

    async Task Because()
    {
        await _pipeline.Handle(Event(new EventType("Unrelated", EventTypeGeneration.First), 1, "p1", ("personId", "p1")));
        _hasNodes = (await _sink.GetNodes("Person")).TotalCount > 0;
    }

    [Fact] void should_not_write_anything() => _hasNodes.ShouldBeFalse();
}
