// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_from_sequence_number;

public class with_event_source_id_filter : given.an_event_sequence
{
    EventSequenceNumber _sequenceNumber;
    EventSourceId _eventSourceId;
    GetFromEventSequenceNumberRequest _request;
    GetFromEventSequenceNumberResponse _response;

    void Establish()
    {
        _sequenceNumber = 42UL;
        _eventSourceId = Guid.NewGuid();

        _response = new()
        {
            Events = []
        };

        _eventSequences
            .When(_ => _.GetEventsFromEventSequenceNumber(Arg.Any<GetFromEventSequenceNumberRequest>(), CallContext.Default))
            .Do(callInfo => _request = callInfo.Arg<GetFromEventSequenceNumberRequest>());

        _eventSequences
            .GetEventsFromEventSequenceNumber(Arg.Any<GetFromEventSequenceNumberRequest>(), CallContext.Default)
            .Returns(_response);
    }

    async Task Because() => await _eventSequence.GetFromSequenceNumber(_sequenceNumber, _eventSourceId);

    [Fact] void should_pass_event_source_id() => _request.EventSourceId.ShouldEqual(_eventSourceId.Value);
    [Fact] void should_pass_sequence_number() => _request.FromEventSequenceNumber.ShouldEqual((ulong)_sequenceNumber);
}
