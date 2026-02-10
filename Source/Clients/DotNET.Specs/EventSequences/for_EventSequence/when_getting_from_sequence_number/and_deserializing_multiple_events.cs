// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_from_sequence_number;

public class and_deserializing_multiple_events : given.an_event_sequence
{
    EventSequenceNumber _sequenceNumber;
    EventType _eventType;
    List<TestEvent> _expectedEvents;
    GetFromEventSequenceNumberResponse _response;
    IImmutableList<AppendedEvent> _result;

    void Establish()
    {
        _sequenceNumber = 42UL;
        _eventType = new(Guid.NewGuid().ToString(), EventTypeGeneration.First);
        _expectedEvents =
        [
            new("Event 1", 1),
            new("Event 2", 2),
            new("Event 3", 3)
        ];

        _eventTypes.GetClrTypeFor(_eventType.Id).Returns(typeof(TestEvent));

        var contractEvents = _expectedEvents.Select((evt, idx) => new Contracts.Events.AppendedEvent
        {
            Context = new Contracts.Events.EventContext
            {
                EventType = _eventType.ToContract(),
                SequenceNumber = (ulong)(_sequenceNumber + idx),
                EventSourceId = Guid.NewGuid().ToString(),
                EventSourceType = EventSourceType.Default,
                EventStreamType = EventStreamType.All,
                EventStreamId = EventStreamId.Default,
                Occurred = DateTimeOffset.UtcNow,
                EventStore = "event-store",
                Namespace = "namespace",
                CorrelationId = Guid.NewGuid(),
                Causation = [],
                CausedBy = new Contracts.Identities.Identity(),
                Tags = [],
                Hash = "hash",
                ObservationState = Contracts.Events.EventObservationState.None
            },
            Content = JsonSerializer.Serialize(evt, JsonSerializerOptions.Default)
        }).ToList();

        _response = new()
        {
            Events = contractEvents
        };

        _eventSequences
            .GetEventsFromEventSequenceNumber(Arg.Any<GetFromEventSequenceNumberRequest>(), CallContext.Default)
            .Returns(_response);
    }

    async Task Because() => _result = await _eventSequence.GetFromSequenceNumber(_sequenceNumber);

    [Fact] void should_return_correct_number_of_events() => _result.Count.ShouldEqual(_expectedEvents.Count);
    [Fact] void should_deserialize_all_events_correctly() => _result.Select(e => (e.Content as TestEvent)?.Name).ShouldEqual(_expectedEvents.Select(e => e.Name));
    [Fact] void should_return_events_with_correct_sequence_numbers() => _result.Select(e => e.Context.SequenceNumber).ShouldEqual(Enumerable.Range(0, _expectedEvents.Count).Select(i => _sequenceNumber + (ulong)i));

    record TestEvent(string Name, int Value);
}
