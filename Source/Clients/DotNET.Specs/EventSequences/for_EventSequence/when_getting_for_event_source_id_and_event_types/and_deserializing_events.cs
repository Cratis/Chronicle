// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_for_event_source_id_and_event_types;

public class and_deserializing_events : given.an_event_sequence
{
    EventSourceId _eventSourceId;
    List<EventType> _eventTypes;
    List<TestEvent> _expectedEvents;
    Contracts.Sequences.ForEventSourceIdAndEventTypesRequest _request;
    IImmutableList<AppendedEvent> _result;

    void Establish()
    {
        _eventSourceId = Guid.NewGuid();
        _eventTypes =
        [
            new(Guid.NewGuid().ToString(), EventTypeGeneration.First),
            new(Guid.NewGuid().ToString(), EventTypeGeneration.First)
        ];

        _expectedEvents =
        [
            new("Event 1", 1),
            new("Event 2", 2)
        ];

        foreach (var eventType in _eventTypes)
        {
            base._eventTypes.HasFor(eventType.Id).Returns(true);
            base._eventTypes.GetClrTypeFor(eventType.Id).Returns(typeof(TestEvent));
        }

        var contractEvents = _expectedEvents.Select((evt, idx) => new Contracts.Sequences.AppendedEventResponse
        {
            Context = new Contracts.Sequences.EventContext
            {
                EventType = _eventTypes[idx].ToSequencesContract(),
                SequenceNumber = (ulong)(42 + idx),
                EventSourceId = _eventSourceId,
                EventSourceType = EventSourceType.Default,
                EventStreamType = EventStreamType.All,
                EventStreamId = EventStreamId.Default,
                Occurred = DateTimeOffset.UtcNow,
                CorrelationId = Guid.NewGuid(),
                Causation = [],
                CausedBy = new Contracts.Sequences.Identity(),
                Tags = []
            },
            Content = JsonSerializer.Serialize(evt, JsonSerializerOptions.Default)
        }).ToList();

        _sequences
            .When(_ => _.ForEventSourceIdAndEventTypes(Arg.Any<Contracts.Sequences.ForEventSourceIdAndEventTypesRequest>(), CallContext.Default))
            .Do(callInfo => _request = callInfo.Arg<Contracts.Sequences.ForEventSourceIdAndEventTypesRequest>());

        _sequences
            .ForEventSourceIdAndEventTypes(Arg.Any<Contracts.Sequences.ForEventSourceIdAndEventTypesRequest>(), CallContext.Default)
            .Returns(QueryResult<IEnumerable<Contracts.Sequences.AppendedEventResponse>>.Success(Guid.NewGuid(), contractEvents));
    }

    async Task Because() => _result = await _eventSequence.GetForEventSourceIdAndEventTypes(_eventSourceId, _eventTypes);

    [Fact] void should_call_service() => _request.ShouldNotBeNull();
    [Fact] void should_pass_event_source_id() => _request.EventSourceId.ShouldEqual(_eventSourceId.Value);
    [Fact] void should_pass_event_types() => _request.EventTypeIds.ShouldEqual(string.Join(',', _eventTypes.Select(_ => _.Id.Value)));
    [Fact] void should_return_correct_number_of_events() => _result.Count.ShouldEqual(_expectedEvents.Count);
    [Fact] void should_deserialize_all_events_correctly() => _result.Select(e => (e.Content as TestEvent)?.Name).ShouldEqual(_expectedEvents.Select(e => e.Name));

    record TestEvent(string Name, int Value);
}
