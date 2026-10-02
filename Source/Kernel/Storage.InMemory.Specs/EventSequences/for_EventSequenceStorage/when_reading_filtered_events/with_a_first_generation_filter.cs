// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_reading_filtered_events;

public class with_a_first_generation_filter : given.a_storage_with_events_of_multiple_generations
{
    IEnumerable<AppendedEvent> _events;

    async Task Because() => _events = await Read(await _storage.GetFromSequenceNumber(0, eventSourceId: _eventSourceId, eventTypes: [_eventType]));

    [Fact] void should_read_all_generations_of_the_requested_type_for_the_source() => _events.Select(_ => _.Context.SequenceNumber.Value).ShouldEqual([0UL, 2UL, 4UL]);
    [Fact] void should_preserve_the_stored_event_types() => _events.Select(_ => _.Context.EventType).ShouldEqual([_eventType, _secondGeneration, _thirdGeneration]);
}
