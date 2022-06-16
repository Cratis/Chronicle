// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.EventSequences.Caching.for_EventSequenceCache.when_creating;

public class new_events_that_falls_outside_the_range_and_before_the_beginning_of_existing_events : given.no_events_in_sequence
{
    IEnumerable<AppendedEvent> events;

    void Establish()
    {
        cache.Feed(Enumerable.Range(0, 100).GenerateEvents(200));

        events = Enumerable.Range(0, 101).GenerateEvents(0);
    }

    void Because() => cache.Feed(events);

    [Fact] void should_have_expected_range() => cache.CurrentRange.ShouldEqual(new(0, 99));
    [Fact] void should_have_expected_number_of_events_in_content() => cache.Content.Count().ShouldEqual(range_size);
}
