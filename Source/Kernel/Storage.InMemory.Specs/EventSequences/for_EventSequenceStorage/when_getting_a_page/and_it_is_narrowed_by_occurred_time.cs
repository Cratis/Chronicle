// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage.when_getting_a_page;

public class and_it_is_narrowed_by_occurred_time : given.a_storage_with_events_spread_over_time
{
    IEnumerable<AppendedEvent> _page;
    EventCount _count;

    async Task Because()
    {
        var criteria = new EventSequenceQueryCriteria(OccurredFrom: _secondDay, OccurredTo: _thirdDay);
        _count = await _storage.GetCountMatching(criteria);

        using var cursor = await _storage.GetPage(criteria, 0, 100);
        await cursor.MoveNext();
        _page = cursor.Current;
    }

    [Fact] void should_count_only_the_events_within_the_range() => _count.Value.ShouldEqual(2UL);
    [Fact] void should_return_only_the_events_within_the_range() => _page.Select(_ => _.Context.SequenceNumber.Value).ShouldEqual([2UL, 3UL]);
}
