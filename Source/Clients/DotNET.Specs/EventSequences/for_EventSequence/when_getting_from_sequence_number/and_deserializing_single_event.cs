// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_from_sequence_number;

public class and_deserializing_single_event : given.an_event_sequence
{
    EventSequenceNumber _sequenceNumber;
    EventType _eventType;
    TestEvent _expectedEvent;
    GetFromEventSequenceNumberRequest _request;
    GetFromEventSequenceNumberResponse _response;
    IImmutableList<AppendedEvent> _result;

    void Establish()
    {
        _sequenceNumber = 42UL;
        _eventType = new(Guid.NewGuid().ToString(), EventTypeGeneration.First);
        _expectedEvent = new("Test Content", 123);

        _eventTypes.GetClrTypeFor(_eventType.Id).Returns(typeof(TestEvent));

        var contractEvent = new Contracts.Events.AppendedEvent
        {
            Context = new Contracts.Events.EventContext
            {
                EventType = _eventType.ToContract(),
                SequenceNumber = _sequenceNumber,
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
            Content = JsonSerializer.Serialize(_expectedEvent, JsonSerializerOptions.Default)
        };

        _response = new()
        {
            Events = [contractEvent]
        };

        _eventSequences
            .When(_ => _.GetEventsFromEventSequenceNumber(Arg.Any<GetFromEventSequenceNumberRequest>(), CallContext.Default))
            .Do(callInfo => _request = callInfo.Arg<GetFromEventSequenceNumberRequest>());

        _eventSequences
            .GetEventsFromEventSequenceNumber(Arg.Any<GetFromEventSequenceNumberRequest>(), CallContext.Default)
            .Returns(_response);
    }

    async Task Because() => _result = await _eventSequence.GetFromSequenceNumber(_sequenceNumber);

    [Fact] void should_call_service() => _request.ShouldNotBeNull();
    [Fact] void should_pass_correct_sequence_number() => _request.FromEventSequenceNumber.ShouldEqual((ulong)_sequenceNumber);
    [Fact] void should_return_one_event() => _result.Count.ShouldEqual(1);
    [Fact] void should_deserialize_content_correctly() => ((_result[0].Content as TestEvent)?.Name).ShouldEqual(_expectedEvent.Name);
    [Fact] void should_deserialize_content_value_correctly() => ((_result[0].Content as TestEvent)?.Value).ShouldEqual(_expectedEvent.Value);
    [Fact] void should_return_event_with_correct_context() => _result[0].Context.SequenceNumber.ShouldEqual(_sequenceNumber);

    record TestEvent(string Name, int Value);
}
