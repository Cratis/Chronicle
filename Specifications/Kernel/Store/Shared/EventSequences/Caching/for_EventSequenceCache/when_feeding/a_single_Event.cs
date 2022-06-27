// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.EventSequences.Caching.for_EventSequenceCache.when_creating;

public class a_single_Event : given.no_events_in_sequence
{
    IEnumerable<AppendedEvent> events;

    void Establish()
    {
        events = Enumerable.Range(0, 1).GenerateEvents(0);
    }

    void Because() => cache.Feed(events);

    [Fact] void should_have_equal_range_to_store() => cache.CurrentRange.ShouldEqual(new(0, 0));
    [Fact] void should_have_expected_number_of_events_in_content() => cache.Content.Count().ShouldEqual(1);
}
