// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_for_event_source_id_and_event_types;

public class with_optional_parameters : given.an_event_sequence
{
    EventSourceId _eventSourceId;
    EventStreamType _eventStreamType;
    EventStreamId _eventStreamId;
    EventSourceType _eventSourceType;
    List<EventType> _eventTypes;
    GetForEventSourceIdAndEventTypesRequest _request;
    GetForEventSourceIdAndEventTypesResponse _response;

    void Establish()
    {
        _eventSourceId = Guid.NewGuid();
        _eventStreamType = "custom-stream-type";
        _eventStreamId = "custom-stream-id";
        _eventSourceType = "custom-source-type";
        _eventTypes =
        [
            new(Guid.NewGuid().ToString(), EventTypeGeneration.First)
        ];

        _response = new()
        {
            Events = []
        };

        _eventSequences
            .When(_ => _.GetForEventSourceIdAndEventTypes(Arg.Any<GetForEventSourceIdAndEventTypesRequest>(), CallContext.Default))
            .Do(callInfo => _request = callInfo.Arg<GetForEventSourceIdAndEventTypesRequest>());

        _eventSequences
            .GetForEventSourceIdAndEventTypes(Arg.Any<GetForEventSourceIdAndEventTypesRequest>(), CallContext.Default)
            .Returns(_response);
    }

    async Task Because() => await _eventSequence.GetForEventSourceIdAndEventTypes(
        _eventSourceId,
        _eventTypes,
        _eventStreamType,
        _eventStreamId,
        _eventSourceType);

    [Fact] void should_pass_event_source_id() => _request.EventSourceId.ShouldEqual(_eventSourceId.Value);
    [Fact] void should_pass_event_stream_type() => _request.EventStreamType.ShouldEqual(_eventStreamType.Value);
    [Fact] void should_pass_event_stream_id() => _request.EventStreamId.ShouldEqual(_eventStreamId.Value);
    [Fact] void should_pass_event_source_type() => _request.EventSourceType.ShouldEqual(_eventSourceType.Value);
    [Fact] void should_pass_event_types() => _request.EventTypes.Select(_ => _.ToClient()).ShouldEqual(_eventTypes);
}
