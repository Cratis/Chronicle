// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage;

public class when_reading_a_range_for_multiple_generations : given.a_storage_with_events_of_multiple_generations
{
    IEnumerable<AppendedEvent> _events;

    async Task Because() => _events = await Read(await _storage.GetRange(1, 3, eventSourceId: _eventSourceId, eventTypes: [_eventType]));

    [Fact] void should_read_the_matching_generation_within_the_range() => _events.Select(_ => _.Context.SequenceNumber.Value).ShouldEqual([2UL]);
}
