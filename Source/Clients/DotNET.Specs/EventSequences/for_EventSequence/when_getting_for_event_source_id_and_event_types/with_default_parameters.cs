// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_for_event_source_id_and_event_types;

public class with_default_parameters : given.an_event_sequence
{
    EventSourceId _eventSourceId;
    List<EventType> _eventTypes;
    Contracts.Sequences.ForEventSourceIdAndEventTypesRequest _request;

    void Establish()
    {
        _eventSourceId = Guid.NewGuid();
        _eventTypes =
        [
            new(Guid.NewGuid().ToString(), EventTypeGeneration.First)
        ];

        _sequences
            .When(_ => _.ForEventSourceIdAndEventTypes(Arg.Any<Contracts.Sequences.ForEventSourceIdAndEventTypesRequest>(), CallContext.Default))
            .Do(callInfo => _request = callInfo.Arg<Contracts.Sequences.ForEventSourceIdAndEventTypesRequest>());

        _sequences
            .ForEventSourceIdAndEventTypes(Arg.Any<Contracts.Sequences.ForEventSourceIdAndEventTypesRequest>(), CallContext.Default)
            .Returns(QueryResult<IEnumerable<Contracts.Sequences.AppendedEventResponse>>.Success(Guid.NewGuid(), []));
    }

    async Task Because() => await _eventSequence.GetForEventSourceIdAndEventTypes(_eventSourceId, _eventTypes);

    [Fact] void should_send_null_event_stream_type() => _request.EventStreamType.ShouldBeNull();
    [Fact] void should_send_null_event_stream_id() => _request.EventStreamId.ShouldBeNull();
}
