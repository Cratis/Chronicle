// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
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
    Contracts.Sequences.TailSequenceNumberRequest _request;
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

        _sequences
            .When(_ => _.TailSequenceNumber(Arg.Any<Contracts.Sequences.TailSequenceNumberRequest>(), CallContext.Default))
            .Do(callInfo => _request = callInfo.Arg<Contracts.Sequences.TailSequenceNumberRequest>());

        _sequences
            .TailSequenceNumber(Arg.Any<Contracts.Sequences.TailSequenceNumberRequest>(), CallContext.Default)
            .Returns(QueryResult<ulong>.Success(Guid.NewGuid(), _expectedSequenceNumber.Value));
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
    [Fact] void should_filter_by_event_types() => _request.EventTypeIds.ShouldEqual(string.Join(',', _eventTypes.Select(_ => _.Id.Value)));
    [Fact] void should_return_correct_sequence_number() => _result.ShouldEqual(_expectedSequenceNumber);
}
