// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_using_a_reserved_name;

public class and_the_sequence_is_new_when_completing_a_stream : given.an_event_sequence_with_a_reserved_name
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _eventSequence.CompleteStream(new ClosedStreamScope(EventSourceId: "source")));

    [Fact] void should_refuse_the_name() => _error.ShouldBeOfExactType<ReservedEventSequenceName>();
    [Fact] void should_not_open_sequence_storage() => _namespaceStorage.DidNotReceive().GetEventSequence(Arg.Any<EventSequenceId>());
    [Fact] void should_not_open_closed_streams_storage() => _namespaceStorage.DidNotReceive().GetClosedStreamsConstraints(Arg.Any<EventSequenceId>());
    [Fact] void should_not_write_state() => StateWrites.ShouldEqual(0);
}
