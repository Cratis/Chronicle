// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_appending_two_events;

public class and_reading_from_the_start : given.an_event_sequence_storage
{
    static readonly EventSourceId _eventSourceId = "some-source";
    readonly List<AppendedEvent> _read = [];

    async Task Because()
    {
        await Append(EventSequenceNumber.First, _eventSourceId);
        await Append(EventSequenceNumber.First + 1, _eventSourceId);

        using var cursor = await _storage.GetFromSequenceNumber(EventSequenceNumber.First);
        while (await cursor.MoveNext())
        {
            _read.AddRange(cursor.Current);
        }
    }

    [Fact] void should_read_both_events() => _read.Count.ShouldEqual(2);
    [Fact] void should_read_the_first_event_first() => _read[0].Context.SequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_read_the_second_event_last() => _read[1].Context.SequenceNumber.ShouldEqual(EventSequenceNumber.First + 1);
}
