// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_appending_an_event;

public class and_it_is_the_first : given.an_event_sequence_storage
{
    static readonly EventSourceId _eventSourceId = "some-source";
    EventCount _count;
    EventSequenceNumber _tail;

    async Task Because()
    {
        await Append(EventSequenceNumber.First, _eventSourceId);
        _count = await _storage.GetCount();
        _tail = await _storage.GetTailSequenceNumber();
    }

    [Fact] void should_have_a_count_of_one() => _count.Value.ShouldEqual(1UL);
    [Fact] void should_have_the_appended_event_as_tail() => _tail.ShouldEqual(EventSequenceNumber.First);
}
