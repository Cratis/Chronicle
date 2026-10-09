// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_completing_a_stream_scope;

public class and_scope_is_covered_by_a_broader_closure : given.an_event_sequence_with_closed_streams
{
    Result<EventSequenceNumber, CompleteStreamError> _result;

    async Task Establish() => await _closures.Close(new(new(EventSourceId: "source"), ClosedStreamOwner.Manual, EventSequenceNumber.First, null));

    async Task Because() => _result = await _eventSequence.CompleteStream(new ClosedStreamScope(EventSourceId: "source", EventStreamId: "month"));

    [Fact] void should_report_already_completed() => _result.AsT1.ShouldEqual(CompleteStreamError.AlreadyCompleted);
    [Fact] async Task should_keep_only_the_broader_closure() => (await _closures.GetAll()).Count().ShouldEqual(1);
}
