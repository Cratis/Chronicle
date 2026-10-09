// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_completing_a_stream_scope;

public class and_scope_is_new : given.an_event_sequence_with_closed_streams
{
    Result<EventSequenceNumber, CompleteStreamError> _result;
    readonly ClosedStreamScope _scope = new(EventSourceId: "source");

    async Task Because() => _result = await _eventSequence.CompleteStream(_scope);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_record_the_scope() => (await _closures.GetAll()).Single().Scope.ShouldEqual(_scope);
    [Fact] async Task should_record_manual_owner() => (await _closures.GetAll()).Single().Owner.ShouldEqual(ClosedStreamOwner.Manual);
    [Fact] async Task should_record_closing_time() => (await _closures.GetAll()).Single().ClosedAt.ShouldNotBeNull();
}
