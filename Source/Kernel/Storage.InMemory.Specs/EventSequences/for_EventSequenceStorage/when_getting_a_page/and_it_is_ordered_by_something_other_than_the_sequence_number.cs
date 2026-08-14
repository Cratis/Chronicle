// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage.when_getting_a_page;

/// <summary>
/// The order is applied in storage rather than to the page, so the first page of a sorted query is
/// the top of the whole matching set - not the first six events re-sorted among themselves.
/// </summary>
public class and_it_is_ordered_by_something_other_than_the_sequence_number : given.a_storage_with_events_spread_over_time
{
    IEnumerable<AppendedEvent> _byEventType;
    IEnumerable<AppendedEvent> _byEventSourceDescending;
    IEnumerable<AppendedEvent> _byOccurredDescending;

    async Task Because()
    {
        _byEventType = await Page(new EventSequenceQuerySort(EventSequenceQuerySortBy.EventType));
        _byEventSourceDescending = await Page(new EventSequenceQuerySort(EventSequenceQuerySortBy.EventSourceId, Descending: true));
        _byOccurredDescending = await Page(new EventSequenceQuerySort(EventSequenceQuerySortBy.Occurred, Descending: true));
    }

    [Fact] void should_start_with_the_first_event_type_alphabetically() =>
        _byEventType.First().Context.EventType.Id.ShouldEqual(_archived.Id);

    [Fact] void should_keep_every_event_of_that_type_together() =>
        _byEventType.Take(2).All(_ => _.Context.EventType.Id == _archived.Id).ShouldBeTrue();

    [Fact] void should_start_with_the_last_event_source_alphabetically() =>
        _byEventSourceDescending.First().Context.EventSourceId.ShouldEqual(_secondEventSourceId);

    [Fact] void should_start_with_the_most_recent_event() =>
        _byOccurredDescending.First().Context.Occurred.ShouldEqual(_thirdDay.AddHours(3));

    [Fact] void should_walk_back_through_time() =>
        _byOccurredDescending.Skip(1).First().Context.Occurred.ShouldEqual(_thirdDay);

    async Task<IEnumerable<AppendedEvent>> Page(EventSequenceQuerySort sort)
    {
        using var cursor = await _storage.GetPage(EventSequenceQueryCriteria.Empty, 0, 6, sort);
        await cursor.MoveNext();

        return cursor.Current;
    }
}
