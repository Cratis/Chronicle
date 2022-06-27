// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.EventSequences.Caching.for_EventSequenceCache.when_creating;

public class new_events_creates_a_gap_before_existing_range : given.a_cache_with_a_set_of_events
{
    protected override EventSequenceCacheRange range => new(100, 200);

    protected override int cursor_size => 10;

    protected override int range_size => 100;
    protected override EventSequenceNumber populate_from => 100;
    static EventSequenceCacheRange expected_range = new(25, 124);

    void Establish()
    {
        storage_provider
            .Setup(_ => _.GetRange(event_sequence_id, 75, 100, null, null))
            .Returns((EventSequenceId _, EventSequenceNumber start, EventSequenceNumber end, EventSourceId? __, IEnumerable<EventType>? ___) =>
                Task.FromResult<IEventCursor>(new FakeEventCursor(start, end, cursor_size)));
    }

    void Because() => cache.Feed(Enumerable.Range(0, 50).GenerateEvents(25));

    [Fact] void should_have_expected_range() => cache.CurrentRange.ShouldEqual(expected_range);
    [Fact] void should_have_expected_number_of_events_in_content() => cache.Content.Count().ShouldEqual(range_size);
    [Fact] void should_contain_only_the_expected_range() => cache.Content.ShouldHaveAllInRange(expected_range);
}
