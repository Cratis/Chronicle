// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_completing_a_stream_scope;

public class and_expected_tail_does_not_match : given.an_event_sequence_with_closed_streams
{
    Result<EventSequenceNumber, CompleteStreamError> _result;

    void Establish() => _eventSequenceStorage.GetTailSequenceNumber(eventSourceId: new EventSourceId("source")).Returns(new EventSequenceNumber(5));

    async Task Because() => _result = await _eventSequence.CompleteStream(new ClosedStreamScope(EventSourceId: "source"), EventSequenceNumber.First);

    [Fact] void should_report_tail_mismatch() => _result.AsT1.ShouldEqual(CompleteStreamError.ExpectedTailMismatch);
    [Fact] async Task should_not_close_the_scope() => (await _closures.GetAll()).ShouldBeEmpty();
}
