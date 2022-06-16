// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.EventSequences.Caching.for_EventSequenceCache.when_creating;

public class new_events_that_falls_partially_within_the_range_and_past_the_end_of_existing_events : given.an_event_store_with_a_set_of_events
{
    protected override EventSequenceCacheRange range => new(0, 100);

    protected override int cursor_size => 10;

    protected override int range_size => 100;

    IEnumerable<AppendedEvent> events;

    void Establish()
    {
        events = Enumerable.Range(0, 51).GenerateEvents(100);
    }

    void Because() => cache.Feed(events);

    [Fact] void should_have_equal_range_to_store() => cache.CurrentRange.ShouldEqual(new(51, 150));
    [Fact] void should_have_expected_number_of_events_in_content() => cache.Content.Count().ShouldEqual(range_size);
}
