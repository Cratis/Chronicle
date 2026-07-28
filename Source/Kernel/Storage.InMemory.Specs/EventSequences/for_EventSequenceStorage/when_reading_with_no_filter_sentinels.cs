// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage;

/// <summary>
/// The kernel asks for "everything" by passing each criterion's sentinel — an unspecified
/// <see cref="EventSourceId"/>, <see cref="EventStreamType.All"/>, the default <see cref="EventStreamId"/>
/// and an empty event type set — never <see langword="null"/>. Treating a sentinel as a value to match on
/// narrows every event away and makes reads silently return nothing.
/// </summary>
public class when_reading_with_no_filter_sentinels : given.a_storage_with_appended_events
{
    int _fromSequenceNumberCount;
    EventSequenceNumber _tail;
    EventCount _count;

    async Task Because()
    {
        using var cursor = await _storage.GetFromSequenceNumber(
            EventSequenceNumber.First,
            EventSourceId.Unspecified,
            EventStreamType.All,
            EventStreamId.Default,
            []);

        while (await cursor.MoveNext())
        {
            _fromSequenceNumberCount += cursor.Current.Count();
        }

        _tail = await _storage.GetTailSequenceNumber([], EventSourceId.Unspecified, EventSourceType.Unspecified, EventStreamId.Default, EventStreamType.All);
        _count = await _storage.GetCount(eventTypes: []);
    }

    [Fact] void should_return_every_event_from_the_first_sequence_number() => _fromSequenceNumberCount.ShouldEqual(3);
    [Fact] void should_report_the_last_appended_sequence_number_as_the_tail() => _tail.ShouldEqual((EventSequenceNumber)2UL);
    [Fact] void should_count_every_event() => ((ulong)_count).ShouldEqual(3UL);
}
