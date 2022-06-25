// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.EventSequences.Caching.for_EventSequenceCache.when_getting_from_sequence_number;

public class and_cache_starts_after_sequence_number : given.no_events_in_sequence
{
    IEventCursor cursor;

    void Establish()
    {
        cache.Feed(Enumerable.Range(0, 100).GenerateEvents(50));
        storage_provider.Setup(_ => _.GetTailSequenceNumber(event_sequence_id, null, null)).Returns(Task.FromResult((EventSequenceNumber)200));
    }

    void Because()
    {
        cursor = cache.GetFrom(0);
        cursor.MoveNext();
    }

    [Fact] void should_receive_all_events() => cursor.Current.ShouldEqual(cache.Content);
}
