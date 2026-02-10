// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_for_event_source_id_and_event_types;

public class with_default_parameters : given.an_event_sequence
{
    EventSourceId _eventSourceId;
    List<EventType> _eventTypes;
    GetForEventSourceIdAndEventTypesRequest _request;
    GetForEventSourceIdAndEventTypesResponse _response;

    void Establish()
    {
        _eventSourceId = Guid.NewGuid();
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

    async Task Because() => await _eventSequence.GetForEventSourceIdAndEventTypes(_eventSourceId, _eventTypes);

    [Fact] void should_use_default_event_stream_type() => _request.EventStreamType.ShouldEqual(EventStreamType.All.Value);
    [Fact] void should_use_default_event_stream_id() => _request.EventStreamId.ShouldEqual(EventStreamId.Default);
    [Fact] void should_use_default_event_source_type() => _request.EventSourceType.ShouldEqual(EventSourceType.Default.Value);
}
