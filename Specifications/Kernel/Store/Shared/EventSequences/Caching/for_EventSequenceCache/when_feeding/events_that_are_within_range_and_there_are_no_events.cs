// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.EventSequences.Caching.for_EventSequenceCache.when_creating;

public class events_that_are_within_range_and_there_are_no_events : given.no_events_in_sequence
{
    IEnumerable<AppendedEvent> events;

    void Establish()
    {
        events = Enumerable.Range(0, 51).GenerateEvents(100);
    }

    void Because() => cache.Feed(events);

    [Fact] void should_have_equal_range_to_store() => cache.CurrentRange.ShouldEqual(new(100, 150));
    [Fact] void should_have_expected_number_of_events_in_content() => cache.Content.Count().ShouldEqual(events.Count());
}
