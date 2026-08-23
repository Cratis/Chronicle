// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_tail_sequence_number;

public class without_filters : given.an_event_sequence
{
    EventSequenceNumber _expectedSequenceNumber;
    Contracts.Sequences.TailSequenceNumberRequest _request;
    EventSequenceNumber _result;

    void Establish()
    {
        _expectedSequenceNumber = 100UL;

        _sequences
            .When(_ => _.TailSequenceNumber(Arg.Any<Contracts.Sequences.TailSequenceNumberRequest>(), CallContext.Default))
            .Do(callInfo => _request = callInfo.Arg<Contracts.Sequences.TailSequenceNumberRequest>());

        _sequences
            .TailSequenceNumber(Arg.Any<Contracts.Sequences.TailSequenceNumberRequest>(), CallContext.Default)
            .Returns(QueryResult<ulong>.Success(Guid.NewGuid(), _expectedSequenceNumber.Value));
    }

    async Task Because() => _result = await _eventSequence.GetTailSequenceNumber();

    [Fact] void should_call_service() => _request.ShouldNotBeNull();
    [Fact] void should_return_correct_sequence_number() => _result.ShouldEqual(_expectedSequenceNumber);
    [Fact] void should_not_filter_by_event_source_id() => string.IsNullOrEmpty(_request.EventSourceId).ShouldBeTrue();
    [Fact] void should_not_filter_by_event_source_type() => string.IsNullOrEmpty(_request.EventSourceType).ShouldBeTrue();
    [Fact] void should_not_filter_by_event_stream_type() => string.IsNullOrEmpty(_request.EventStreamType).ShouldBeTrue();
    [Fact] void should_not_filter_by_event_stream_id() => string.IsNullOrEmpty(_request.EventStreamId).ShouldBeTrue();
    [Fact] void should_not_filter_by_event_types() => string.IsNullOrEmpty(_request.EventTypeIds).ShouldBeTrue();
}
