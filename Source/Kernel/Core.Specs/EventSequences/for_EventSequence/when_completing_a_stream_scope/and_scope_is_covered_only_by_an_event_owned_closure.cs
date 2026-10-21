// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_completing_a_stream_scope;

public class and_scope_is_covered_only_by_an_event_owned_closure : given.an_event_sequence_with_closed_streams
{
    Result<EventSequenceNumber, CompleteStreamError> _result;
    readonly ClosedStreamScope _scope = new(EventSourceId: "source", EventStreamId: "month");

    async Task Establish() => await _closures.Close(new(new(EventSourceId: "source"), "closing-constraint", EventSequenceNumber.First, null));

    async Task Because() => _result = await _eventSequence.CompleteStream(_scope);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_record_an_independent_manual_closure() => (await _closures.GetForOwner(ClosedStreamOwner.Manual)).Single().Scope.ShouldEqual(_scope);
}
