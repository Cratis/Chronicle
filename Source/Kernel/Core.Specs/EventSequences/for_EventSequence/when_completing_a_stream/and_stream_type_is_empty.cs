// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_completing_a_stream;

public class and_stream_type_is_empty : when_completing_a_stream_scope.given.an_event_sequence_with_closed_streams
{
    Result<EventSequenceNumber, CompleteStreamError> _result;

    async Task Because() => _result = await _eventSequence.CompleteStream(new EventStreamType(string.Empty), "month");

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_close_only_the_all_stream_type() => (await _closures.GetAll()).Single().Scope.ShouldEqual(new ClosedStreamScope(EventStreamType: EventStreamType.All, EventStreamId: "month"));
    [Fact] async Task should_leave_other_stream_types_open() => (await _eventSequence.IsStreamCompleted("transactions", "month")).ShouldBeFalse();
}
