// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.EventSequences.Caching.for_EventSequenceCache.when_creating;

public class too_many_events : given.no_events_in_sequence
{
    IEnumerable<AppendedEvent> events;

    void Establish()
    {
        events = Enumerable.Range(0, range_size + 20).GenerateEvents(0);
    }

    void Because() => cache.Feed(events);

    [Fact] void should_have_equal_range_to_store() => cache.CurrentRange.ShouldEqual(new(0, (ulong)range_size));
    [Fact] void should_have_expected_number_of_events_in_content() => cache.Content.Count().ShouldEqual(range_size + 1);
    [Fact] void should_have_the_events_that_fit_in_the_range_size() => cache.Content.ShouldContainOnly(events.Take(range_size + 1));
}
