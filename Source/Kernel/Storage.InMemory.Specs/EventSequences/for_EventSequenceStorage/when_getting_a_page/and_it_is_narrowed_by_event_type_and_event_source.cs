// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage.when_getting_a_page;

public class and_it_is_narrowed_by_event_type_and_event_source : given.a_storage_with_events_spread_over_time
{
    IEnumerable<AppendedEvent> _byEventType;
    IEnumerable<AppendedEvent> _byEventSource;
    IEnumerable<AppendedEvent> _byTag;

    async Task Because()
    {
        _byEventType = await Page(new EventSequenceQueryCriteria(EventTypes: [_archived]));
        _byEventSource = await Page(new EventSequenceQueryCriteria(_firstEventSourceId));
        _byTag = await Page(new EventSequenceQueryCriteria(Tags: [new Tag("important")]));
    }

    [Fact] void should_return_only_events_of_that_type() => _byEventType.Select(_ => _.Context.SequenceNumber.Value).ShouldEqual([2UL, 5UL]);
    [Fact] void should_return_only_events_from_that_event_source() => _byEventSource.Select(_ => _.Context.SequenceNumber.Value).ShouldEqual([0UL, 2UL, 4UL]);
    [Fact] void should_return_only_events_carrying_that_tag() => _byTag.Select(_ => _.Context.SequenceNumber.Value).ShouldEqual([0UL, 3UL]);

    async Task<IEnumerable<AppendedEvent>> Page(EventSequenceQueryCriteria criteria)
    {
        using var cursor = await _storage.GetPage(criteria, 0, 100);
        await cursor.MoveNext();
        return cursor.Current.ToArray();
    }
}
