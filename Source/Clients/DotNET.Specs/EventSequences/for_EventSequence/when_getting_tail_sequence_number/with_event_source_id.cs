// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_tail_sequence_number;

public class with_event_source_id : given.an_event_sequence
{
    EventSourceId _eventSourceId;
    GetTailSequenceNumberRequest _request;
    EventSequenceNumber _expectedSequenceNumber;
    EventSequenceNumber _result;

    void Establish()
    {
        _eventSourceId = Guid.NewGuid();
        _expectedSequenceNumber = 50UL;

        _eventSequences
            .When(_ => _.GetTailSequenceNumber(Arg.Any<GetTailSequenceNumberRequest>(), CallContext.Default))
            .Do(callInfo => _request = callInfo.Arg<GetTailSequenceNumberRequest>());

        _eventSequences
            .GetTailSequenceNumber(Arg.Any<GetTailSequenceNumberRequest>(), CallContext.Default)
            .Returns(new GetTailSequenceNumberResponse { SequenceNumber = _expectedSequenceNumber });
    }

    async Task Because() => _result = await _eventSequence.GetTailSequenceNumber(_eventSourceId);

    [Fact] void should_filter_by_event_source_id() => _request.EventSourceId.ShouldEqual(_eventSourceId.Value);
    [Fact] void should_return_correct_sequence_number() => _result.ShouldEqual(_expectedSequenceNumber);
}
