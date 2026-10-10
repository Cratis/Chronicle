// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_completing_a_stream_scope;

public class and_sequence_is_empty : given.an_event_sequence_with_closed_streams
{
    Result<EventSequenceNumber, CompleteStreamError> _result;

    async Task Because() => _result = await _eventSequence.CompleteStream(new(EventSourceId: "source"));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_report_unavailable_sequence_number() => _result.AsT0.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] async Task should_store_unavailable_sequence_number() => (await _closures.GetAll()).Single().SequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
}
