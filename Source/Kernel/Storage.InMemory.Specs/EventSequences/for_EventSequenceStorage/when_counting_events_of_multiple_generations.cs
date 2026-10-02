// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage;

public class when_counting_events_of_multiple_generations : given.a_storage_with_events_of_multiple_generations
{
    EventCount _count;

    async Task Because() => _count = await _storage.GetCount(lastEventSequenceNumber: 3, eventTypes: [_eventType]);

    [Fact] void should_count_all_generations_within_the_sequence_number_limit() => _count.ShouldEqual((EventCount)2);
}
