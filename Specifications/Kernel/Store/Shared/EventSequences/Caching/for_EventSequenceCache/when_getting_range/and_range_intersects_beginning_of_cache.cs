// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.EventSequences.Caching.for_EventSequenceCache.when_getting_range;

public class and_range_intersects_beginning_of_cache : given.no_events_in_sequence
{
    IEventCursor cursor;

    void Establish()
    {
        cache.Feed(Enumerable.Range(0, 100).GenerateEvents(50));
        storage_provider.Setup(_ => _.GetTailSequenceNumber(event_sequence_id, null, null)).Returns(Task.FromResult((EventSequenceNumber)200));
    }

    void Because()
    {
        cursor = cache.GetRange(0, 75);
        cursor.MoveNext();
    }

    [Fact] void should_receive_the_beginning_of_the_cache() => cursor.Current.ShouldContainOnly(cache.Content.Take(26));
}
