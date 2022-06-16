// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.EventSequences.Caching.for_EventSequenceCache.when_creating;

public class and_there_are_events_that_fall_outside_the_default_range : given.a_set_of_events
{
    protected override EventSequenceCacheRange range => new(0, 120);
    protected override int cursor_size => 10;
    protected override int range_size => 100;

    EventSequenceCache cache;

    void Because() => cache = new(event_sequence_id, range_size, storage_provider.Object);

    [Fact] void should_have_range_set_to_first_and_last_of_allowed_range() => cache.CurrentRange.ShouldEqual(new(21, 120));
    [Fact] void should_have_expected_number_of_events_in_content() => cache.Content.Count().ShouldEqual(range_size);
}
