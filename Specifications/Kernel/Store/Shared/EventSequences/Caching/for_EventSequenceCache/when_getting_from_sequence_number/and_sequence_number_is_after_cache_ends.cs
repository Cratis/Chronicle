// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.EventSequences.Caching.for_EventSequenceCache.when_getting_from_Sequence_number;

public class and_sequence_number_is_after_cache_ends : given.a_cache_with_a_set_of_events
{
    protected override EventSequenceCacheRange range => new(0, 100);

    protected override int cursor_size => 10;

    protected override int range_size => 100;

    IEventCursor cursor;

    void Because() => cursor = cache.GetFrom(200);

    [Fact] void should_have_an_empty_current() => cursor.Current.ShouldBeEmpty();
}
