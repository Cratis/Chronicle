// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceCache;

public class when_adding_three_events_unordered : given.an_event_sequence_cache
{
    AppendedEvent first_event;
    AppendedEvent second_event;
    AppendedEvent third_event;

    AppendedEvent[] events;
    AppendedEvent[] events_by_date;

    void Establish()
    {
        first_event = AppendedEvent.EmptyWithEventSequenceNumber(42L);
        second_event = AppendedEvent.EmptyWithEventSequenceNumber(45L);
        third_event = AppendedEvent.EmptyWithEventSequenceNumber(36L);
    }

    void Because()
    {
        cache.Add(first_event);
        Task.Delay(10).Wait();
        cache.Add(second_event);
        Task.Delay(10).Wait();
        cache.Add(third_event);

        events = cache.Events.ToArray();
        events_by_date = cache.EventsByDate.Select(_ => _.Event).ToArray();
    }

    [Fact] void should_have_three_events() => cache.Count.ShouldEqual(3);
    [Fact] void should_have_third_event_first() => events[0].ShouldEqual(third_event);
    [Fact] void should_have_first_event_second() => events[1].ShouldEqual(first_event);
    [Fact] void should_have_second_event_third() => events[2].ShouldEqual(second_event);
    [Fact] void should_have_first_event_as_first_in_date_order() => events_by_date[0].ShouldEqual(first_event);
    [Fact] void should_have_second_event_as_second_in_date_order() => events_by_date[1].ShouldEqual(second_event);
    [Fact] void should_have_third_event_as_third_in_date_order() => events_by_date[2].ShouldEqual(third_event);
}
