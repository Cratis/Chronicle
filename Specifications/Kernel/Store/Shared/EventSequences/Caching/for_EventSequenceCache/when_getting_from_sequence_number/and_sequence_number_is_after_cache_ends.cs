// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.EventSequences.Caching.for_EventSequenceCache.when_getting_from_sequence_number;

public class and_sequence_number_is_after_cache_ends : given.a_cache_with_a_set_of_events
{
    protected override EventSequenceCacheRange range => new(0, 100);

    protected override int cursor_size => 10;

    protected override int range_size => 100;

    IEventCursor cursor;

    void Establish() => storage_provider.Setup(_ => _.GetTailSequenceNumber(event_sequence_id, null, null)).Returns(Task.FromResult((EventSequenceNumber)200));

    void Because()
    {
        cursor = cache.GetFrom(200);
        cursor.MoveNext();
    }

    [Fact] void should_have_an_empty_current() => cursor.Current.ShouldBeEmpty();
}
