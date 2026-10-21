// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_completing_a_stream_scope;

public class and_scope_is_empty : given.an_event_sequence_with_closed_streams
{
    Result<EventSequenceNumber, CompleteStreamError> _result;

    async Task Because() => _result = await _eventSequence.CompleteStream(new ClosedStreamScope(EventSourceId.Unspecified, EventSourceType.Unspecified));

    [Fact] void should_report_empty_scope() => _result.AsT1.ShouldEqual(CompleteStreamError.EmptyScope);
    [Fact] async Task should_not_close_any_scope() => (await _closures.GetAll()).ShouldBeEmpty();
}
