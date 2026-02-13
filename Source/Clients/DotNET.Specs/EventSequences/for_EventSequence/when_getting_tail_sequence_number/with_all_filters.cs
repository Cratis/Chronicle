// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_tail_sequence_number;

public class with_all_filters : given.an_event_sequence
{
    EventSourceId _eventSourceId;
    EventSourceType _eventSourceType;
    EventStreamType _eventStreamType;
    EventStreamId _eventStreamId;
    List<EventType> _eventTypes;
    GetTailSequenceNumberRequest _request;
    EventSequenceNumber _expectedSequenceNumber;
    EventSequenceNumber _result;

    void Establish()
    {
        _eventSourceId = Guid.NewGuid();
        _eventSourceType = "custom-source-type";
        _eventStreamType = "custom-stream-type";
        _eventStreamId = "custom-stream-id";
        _eventTypes =
        [
            new(Guid.NewGuid().ToString(), EventTypeGeneration.First),
            new(Guid.NewGuid().ToString(), EventTypeGeneration.First)
        ];
        _expectedSequenceNumber = 75UL;

        _eventSequences
            .When(_ => _.GetTailSequenceNumber(Arg.Any<GetTailSequenceNumberRequest>(), CallContext.Default))
            .Do(callInfo => _request = callInfo.Arg<GetTailSequenceNumberRequest>());

        _eventSequences
            .GetTailSequenceNumber(Arg.Any<GetTailSequenceNumberRequest>(), CallContext.Default)
            .Returns(new GetTailSequenceNumberResponse { SequenceNumber = _expectedSequenceNumber });
    }

    async Task Because() => _result = await _eventSequence.GetTailSequenceNumber(
        _eventSourceId,
        _eventSourceType,
        _eventStreamType,
        _eventStreamId,
        _eventTypes);

    [Fact] void should_filter_by_event_source_id() => _request.EventSourceId.ShouldEqual(_eventSourceId.Value);
    [Fact] void should_filter_by_event_source_type() => _request.EventSourceType.ShouldEqual(_eventSourceType.Value);
    [Fact] void should_filter_by_event_stream_type() => _request.EventStreamType.ShouldEqual(_eventStreamType.Value);
    [Fact] void should_filter_by_event_stream_id() => _request.EventStreamId.ShouldEqual(_eventStreamId.Value);
    [Fact] void should_filter_by_event_types() => _request.EventTypes.Select(_ => _.ToClient()).ShouldEqual(_eventTypes);
    [Fact] void should_return_correct_sequence_number() => _result.ShouldEqual(_expectedSequenceNumber);
}
