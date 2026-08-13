// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage.when_getting_a_page;

public class and_it_is_ordered_descending : given.a_storage_with_events_spread_over_time
{
    IEnumerable<AppendedEvent> _page;

    async Task Because()
    {
        using var cursor = await _storage.GetPage(EventSequenceQueryCriteria.Empty, 0, 2, descending: true);
        await cursor.MoveNext();
        _page = cursor.Current;
    }

    [Fact] void should_start_at_the_newest_event() => _page.First().Context.SequenceNumber.ShouldEqual((EventSequenceNumber)5UL);
    [Fact] void should_walk_backwards() => _page.Last().Context.SequenceNumber.ShouldEqual((EventSequenceNumber)4UL);
}
