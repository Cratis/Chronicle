// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation;

public class when_filtering_on_event_types : Specification
{
    EventType event_type_we_want_to_observe;
    EventType event_type_we_want_to_observe_with_different_generation;
    EventType event_type_we_do_not_want_to_observe;

    AppendedEvent[] events;

    AppendedEvent[] filtered_events;

    void Establish()
    {
        event_type_we_want_to_observe = new EventType(Guid.NewGuid(), 1);
        event_type_we_want_to_observe_with_different_generation = new EventType(event_type_we_want_to_observe.Id, 2);
        event_type_we_do_not_want_to_observe = new EventType(Guid.NewGuid(), 1);

        var eventTypes = new[]
        {
            event_type_we_want_to_observe,
            event_type_we_do_not_want_to_observe,
            event_type_we_want_to_observe_with_different_generation
        };

        events = Enumerable.Range(0, 10).Select(_ => AppendedEvent.EmptyWithEventType(eventTypes[Random.Shared.Next(eventTypes.Length)])).ToArray();
    }

    void Because() => filtered_events = events.Where(_ => Observer.EventTypesFilter(null!, new[] { event_type_we_want_to_observe }, _)).ToArray();

    [Fact] void should_only_have_events_of_the_specified_type() => filtered_events.Any(_ => _.Metadata.Type.Id == event_type_we_do_not_want_to_observe.Id).ShouldBeFalse();
}
