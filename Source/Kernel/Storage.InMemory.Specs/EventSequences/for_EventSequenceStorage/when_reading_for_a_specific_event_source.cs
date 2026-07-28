// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage;

public class when_reading_for_a_specific_event_source : given.a_storage_with_appended_events
{
    int _count;
    EventSequenceNumber _tail;

    async Task Because()
    {
        using var cursor = await _storage.GetFromSequenceNumber(
            EventSequenceNumber.First,
            _firstEventSourceId,
            EventStreamType.All,
            EventStreamId.Default,
            []);

        while (await cursor.MoveNext())
        {
            _count += cursor.Current.Count();
        }

        _tail = await _storage.GetTailSequenceNumber([], _firstEventSourceId, EventSourceType.Unspecified, EventStreamId.Default, EventStreamType.All);
    }

    [Fact] void should_return_only_that_event_sources_events() => _count.ShouldEqual(2);
    [Fact] void should_report_that_event_sources_last_sequence_number_as_the_tail() => _tail.ShouldEqual((EventSequenceNumber)2UL);
}
