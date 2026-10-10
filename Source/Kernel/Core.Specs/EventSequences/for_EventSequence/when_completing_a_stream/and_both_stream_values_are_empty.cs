// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_completing_a_stream;

public class and_both_stream_values_are_empty : when_completing_a_stream_scope.given.an_event_sequence_with_closed_streams
{
    Result<EventSequenceNumber, CompleteStreamError> _result;

    async Task Because() => _result = await _eventSequence.CompleteStream(new EventStreamType(string.Empty), new EventStreamId(string.Empty));

    [Fact] void should_refuse_the_default_stream() => _result.AsT1.ShouldEqual(CompleteStreamError.DefaultStreamCannotBeCompleted);
    [Fact] async Task should_not_store_a_closure() => (await _closures.GetAll()).ShouldBeEmpty();
}
