// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_getting_tail_sequence_numbers_for_event_types;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_events_span_multiple_types(ReplicaSetMongoDBFixture fixture) : given.a_replica_set_event_sequence_storage(fixture)
{
    EventType _otherEventType;
    IImmutableDictionary<EventType, EventSequenceNumber> _tails;

    async Task Establish()
    {
        _otherEventType = new EventType("other-event", EventTypeGeneration.First);
        await _storage.AppendMany(
        [
            EventAt(EventSequenceNumber.First, _eventType),
            EventAt(EventSequenceNumber.First + 1, _otherEventType),
            EventAt(EventSequenceNumber.First + 2, _eventType),
            EventAt(EventSequenceNumber.First + 3, _otherEventType)
        ]);
    }

    async Task Because() => _tails = await _storage.GetTailSequenceNumbersForEventTypes([_eventType, _otherEventType]);

    [Fact] void should_return_the_highest_sequence_number_for_the_first_type() => _tails[_eventType].ShouldEqual((EventSequenceNumber)2);
    [Fact] void should_return_the_highest_sequence_number_for_the_second_type() => _tails[_otherEventType].ShouldEqual((EventSequenceNumber)3);
}
