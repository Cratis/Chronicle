// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendResult;

public class when_creating_success_result : Specification
{
    CorrelationId _correlationId;
    EventSequenceNumber _sequenceNumber;
    AppendResult _result;

    void Establish()
    {
        _correlationId = CorrelationId.New();
        _sequenceNumber = 42UL;
    }

    void Because() => _result = AppendResult.Success(_correlationId, _sequenceNumber);

    [Fact] void should_set_sequence_number() => _result.SequenceNumber.ShouldEqual(_sequenceNumber);
    [Fact] void should_set_tail_sequence_number() => _result.TailSequenceNumber.ShouldEqual(_sequenceNumber);
}
