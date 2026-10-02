// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage;

public class when_finding_the_tail_for_multiple_generations : given.a_storage_with_events_of_multiple_generations
{
    EventSequenceNumber _tail;

    async Task Because() => _tail = await _storage.GetTailSequenceNumber([_eventType], _eventSourceId);

    [Fact] void should_find_the_last_event_of_the_type_for_the_source_regardless_of_generation() => _tail.ShouldEqual((EventSequenceNumber)4);
}
