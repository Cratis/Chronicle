// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class many_events_for_different_event_source_ids_with_custom_stream_properties : given.an_event_sequence
{
    List<EventForEventSourceId> _events;
    EventType _eventType;
    JsonObject _eventContext;
    IEnumerable<Causation> _causation;
    Identity _causedBy;
    Contracts.Sequences.AppendManyForEventSourcesRequest _command;
    Contracts.Sequences.AppendManyResponse _response;

    void Establish()
    {
        _eventType = new(Guid.NewGuid().ToString(), EventTypeGeneration.First);
        _eventContext = [];
        _eventSerializer.Serialize(Arg.Any<string>()).Returns(_eventContext);

        var causation = new Causation(DateTimeOffset.UtcNow, "type", new Dictionary<string, string>());

        _events =
        [
            new EventForEventSourceId(Guid.NewGuid(), "Event1", causation)
            {
                EventStreamType = "custom-stream-type",
                EventStreamId = "custom-stream-id",
                EventSourceType = "custom-source-type"
            },
            new EventForEventSourceId(Guid.NewGuid(), "Event2", causation)
            {
                EventStreamType = "another-stream-type",
                EventStreamId = "another-stream-id",
                EventSourceType = "another-source-type"
            }
        ];

        _causation =
        [
            new Causation(DateTimeOffset.UtcNow, Guid.NewGuid().ToString(), new Dictionary<string, string>())
        ];

        _causedBy = new("Subject", "Name", "UserName", new("BehalfOf_Subject", "BehalfOf_Name", "BehalfOf_UserName"));

        _eventTypes.HasFor(typeof(string)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(string)).Returns(_eventType);
        _sequences
            .When(_ => _.AppendManyForEventSources(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesRequest>(), CallContext.Default))
            .Do(callInfo => _command = callInfo.Arg<Contracts.Sequences.AppendManyForEventSourcesRequest>());
        _causationManager.GetCurrentChain().Returns(_causation.ToImmutableList());
        _identityProvider.GetCurrent().Returns(_causedBy);

        _response = new()
        {
            CorrelationId = Guid.NewGuid(),
            SequenceNumbers = [42, 43],
            ConstraintViolations = [],
            Errors = [],
            ConcurrencyViolations = []
        };

        _sequences.AppendManyForEventSources(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesRequest>(), CallContext.Default)
            .Returns(CommandResult<Contracts.Sequences.AppendManyResponse>.Success(Guid.NewGuid(), _response));
    }

    async Task Because() => await _eventSequence.AppendMany(_events);

    [Fact] void should_append_first_event_with_custom_stream_type() => _command.Events.ElementAt(0).EventStreamType.ShouldEqual(_events[0].EventStreamType.Value);
    [Fact] void should_append_first_event_with_custom_stream_id() => _command.Events.ElementAt(0).EventStreamId.ShouldEqual(_events[0].EventStreamId.Value);
    [Fact] void should_append_first_event_with_custom_source_type() => _command.Events.ElementAt(0).EventSourceType.ShouldEqual(_events[0].EventSourceType.Value);
    [Fact] void should_append_second_event_with_another_stream_type() => _command.Events.ElementAt(1).EventStreamType.ShouldEqual(_events[1].EventStreamType.Value);
    [Fact] void should_append_second_event_with_another_stream_id() => _command.Events.ElementAt(1).EventStreamId.ShouldEqual(_events[1].EventStreamId.Value);
    [Fact] void should_append_second_event_with_another_source_type() => _command.Events.ElementAt(1).EventSourceType.ShouldEqual(_events[1].EventSourceType.Value);
}
