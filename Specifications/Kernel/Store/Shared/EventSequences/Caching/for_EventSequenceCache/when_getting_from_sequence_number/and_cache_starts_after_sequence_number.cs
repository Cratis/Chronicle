// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.EventSequences.Caching.for_EventSequenceCache.when_getting_from_Sequence_number;

public class and_cache_starts_after_sequence_number : given.no_events_in_sequence
{
    IEventCursor cursor;

    void Establish()
    {
        cache.Feed(Enumerable.Range(0, 100).GenerateEvents(50));
    }

    void Because() => cursor = cache.GetFrom(0);

    [Fact] void should_receive_all_events() => cursor.Current.ShouldEqual(cache.Content);
}
