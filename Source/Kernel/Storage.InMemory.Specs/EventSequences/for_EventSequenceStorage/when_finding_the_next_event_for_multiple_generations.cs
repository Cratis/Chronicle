// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage;

public class when_finding_the_next_event_for_multiple_generations : given.a_storage_with_events_of_multiple_generations
{
    EventSequenceNumber _next;

    async Task Because() => _next = await _storage.GetNextSequenceNumberGreaterOrEqualThan(1, [_eventType], _eventSourceId);

    [Fact] void should_find_the_next_event_regardless_of_generation() => _next.ShouldEqual((EventSequenceNumber)2);
}
