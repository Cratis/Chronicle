// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage;

public class when_reading_with_a_limit_for_multiple_generations : given.a_storage_with_events_of_multiple_generations
{
    IEnumerable<AppendedEvent> _events;

    async Task Because() => _events = await Read(await _storage.GetEventsWithLimit(1, 2, eventSourceId: _eventSourceId, eventTypes: [_eventType]));

    [Fact] void should_read_the_requested_number_of_events_across_generations() => _events.Select(_ => _.Context.SequenceNumber.Value).ShouldEqual([2UL, 4UL]);
}
