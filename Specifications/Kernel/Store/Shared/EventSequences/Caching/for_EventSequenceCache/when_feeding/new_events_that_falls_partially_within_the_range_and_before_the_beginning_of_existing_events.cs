// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.EventSequences.Caching.for_EventSequenceCache.when_creating;

public class new_events_that_falls_partially_within_the_range_and_before_the_beginning_of_existing_events : given.no_events_in_sequence
{
    static EventSequenceCacheRange expected_range = new(50, 149);
    IEnumerable<AppendedEvent> events;

    void Establish()
    {
        cache.Feed(Enumerable.Range(0, 100).GenerateEvents(100));

        events = Enumerable.Range(0, 51).GenerateEvents(50);
    }

    void Because() => cache.Feed(events);

    [Fact] void should_have_expected_range() => cache.CurrentRange.ShouldEqual(expected_range);
    [Fact] void should_have_expected_number_of_events_in_content() => cache.Content.Count().ShouldEqual(range_size);
    [Fact] void should_contain_only_the_expected_range() => cache.Content.ShouldHaveAllInRange(expected_range);
}
