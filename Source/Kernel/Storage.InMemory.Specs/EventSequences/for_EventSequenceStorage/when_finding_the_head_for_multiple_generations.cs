// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage;

public class when_finding_the_head_for_multiple_generations : given.a_storage_with_events_of_multiple_generations
{
    EventSequenceNumber _head;

    async Task Because() => _head = await _storage.GetHeadSequenceNumber([_secondGeneration], _eventSourceId);

    [Fact] void should_find_the_first_event_regardless_of_generation() => _head.ShouldEqual((EventSequenceNumber)0);
}
