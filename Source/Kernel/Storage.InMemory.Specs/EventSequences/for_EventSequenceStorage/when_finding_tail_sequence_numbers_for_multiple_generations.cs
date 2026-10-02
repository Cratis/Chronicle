// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage;

public class when_finding_tail_sequence_numbers_for_multiple_generations : given.a_storage_with_events_of_multiple_generations
{
    TailEventSequenceNumbers _tails;

    async Task Because() => _tails = await _storage.GetTailSequenceNumbers([_eventType]);

    [Fact] void should_find_the_tail_of_the_sequence() => _tails.Tail.ShouldEqual((EventSequenceNumber)6);
    [Fact] void should_find_the_tail_of_the_requested_type_across_generations() => _tails.TailForEventTypes.ShouldEqual((EventSequenceNumber)5);
    [Fact] void should_preserve_the_requested_event_types() => _tails.EventTypes.ShouldEqual([_eventType]);
}
