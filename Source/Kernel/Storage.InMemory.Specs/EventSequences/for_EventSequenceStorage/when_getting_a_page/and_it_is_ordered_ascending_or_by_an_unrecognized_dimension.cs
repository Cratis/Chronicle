// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage.when_getting_a_page;

public class and_it_is_ordered_ascending_or_by_an_unrecognized_dimension : given.a_storage_with_events_spread_over_time
{
    IEnumerable<AppendedEvent> _byOccurred;
    IEnumerable<AppendedEvent> _byEventTypeDescending;
    IEnumerable<AppendedEvent> _byEventSource;
    IEnumerable<AppendedEvent> _byUnrecognized;
    IEnumerable<AppendedEvent> _byUnrecognizedDescending;

    async Task Because()
    {
        _byOccurred = await Page(new EventSequenceQuerySort(EventSequenceQuerySortBy.Occurred));
        _byEventTypeDescending = await Page(new EventSequenceQuerySort(EventSequenceQuerySortBy.EventType, Descending: true));
        _byEventSource = await Page(new EventSequenceQuerySort(EventSequenceQuerySortBy.EventSourceId));
        _byUnrecognized = await Page(new EventSequenceQuerySort((EventSequenceQuerySortBy)99));
        _byUnrecognizedDescending = await Page(new EventSequenceQuerySort((EventSequenceQuerySortBy)99, Descending: true));
    }

    [Fact] void should_order_by_occurred_oldest_first() => _byOccurred.Select(_ => _.Context.SequenceNumber.Value).ShouldEqual([0UL, 1UL, 2UL, 3UL, 4UL, 5UL]);
    [Fact] void should_start_with_the_last_event_type_alphabetically_when_descending() => _byEventTypeDescending.First().Context.EventType.Id.ShouldEqual(_registered.Id);
    [Fact] void should_start_with_the_first_event_source_alphabetically() => _byEventSource.First().Context.EventSourceId.ShouldEqual(_firstEventSourceId);
    [Fact] void should_order_by_sequence_number_for_an_unrecognized_dimension() => _byUnrecognized.Select(_ => _.Context.SequenceNumber.Value).ShouldEqual([0UL, 1UL, 2UL, 3UL, 4UL, 5UL]);
    [Fact] void should_order_by_sequence_number_descending_for_an_unrecognized_dimension() => _byUnrecognizedDescending.Select(_ => _.Context.SequenceNumber.Value).ShouldEqual([5UL, 4UL, 3UL, 2UL, 1UL, 0UL]);

    async Task<IEnumerable<AppendedEvent>> Page(EventSequenceQuerySort sort)
    {
        using var cursor = await _storage.GetPage(EventSequenceQueryCriteria.Empty, 0, 6, sort);
        await cursor.MoveNext();

        return cursor.Current;
    }
}
