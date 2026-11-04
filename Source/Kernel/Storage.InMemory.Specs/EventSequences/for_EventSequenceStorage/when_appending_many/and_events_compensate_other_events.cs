// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_appending_many;

public class and_events_compensate_other_events : given.an_event_sequence_storage
{
    AppendedEvent _compensation;
    AppendedEvent _ordinary;

    async Task Because()
    {
        static EventToAppendToStorage Create(ulong sequenceNumber, EventSequenceNumber? compensationFor) => new(
            new EventSequenceNumber(sequenceNumber),
            EventSourceType.Default,
            "source",
            EventStreamType.All,
            EventStreamId.Default,
            new EventType("some-event", EventTypeGeneration.First),
            CorrelationId.New(),
            [],
            [],
            [],
            DateTimeOffset.UtcNow,
            new ExpandoObject(),
            EventHash.NotSet)
        {
            CompensationFor = compensationFor
        };

        await _storage.AppendMany([Create(0, null), Create(1, new EventSequenceNumber(0))]);
        _ordinary = await _storage.GetEventAt(EventSequenceNumber.First);
        _compensation = await _storage.GetEventAt(new EventSequenceNumber(1));
    }

    [Fact] void should_keep_the_compensated_sequence_number_on_the_event_context() => _compensation.Context.CompensationFor.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_read_an_ordinary_event_without_a_compensated_sequence_number() => _ordinary.Context.CompensationFor.ShouldBeNull();
}
