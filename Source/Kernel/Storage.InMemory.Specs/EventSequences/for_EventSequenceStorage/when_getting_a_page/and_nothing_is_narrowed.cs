// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage.when_getting_a_page;

public class and_nothing_is_narrowed : given.a_storage_with_events_spread_over_time
{
    IEnumerable<AppendedEvent> _page;
    EventCount _count;

    async Task Because()
    {
        _count = await _storage.GetCountMatching(EventSequenceQueryCriteria.Empty);
        using var cursor = await _storage.GetPage(EventSequenceQueryCriteria.Empty, 2, 2);
        await cursor.MoveNext();
        _page = cursor.Current;
    }

    [Fact] void should_count_every_event() => _count.Value.ShouldEqual(6UL);
    [Fact] void should_return_the_requested_page_size() => _page.Count().ShouldEqual(2);
    [Fact] void should_skip_into_the_sequence() => _page.First().Context.SequenceNumber.ShouldEqual((EventSequenceNumber)2UL);
    [Fact] void should_order_oldest_first() => _page.Last().Context.SequenceNumber.ShouldEqual((EventSequenceNumber)3UL);
}
