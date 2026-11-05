// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_completing_a_stream;

public class and_the_scope_is_too_long : when_completing_a_stream_scope.given.an_event_sequence_with_closed_streams
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _eventSequence.CompleteStream("transactions", new EventStreamId(new string('p', 256))));

    [Fact] void should_refuse_the_completion() => _error.ShouldBeOfExactType<ClosedStreamScopeTooLong>();
    [Fact] async Task should_not_close_anything() => (await _closures.GetAll()).ShouldBeEmpty();
}
