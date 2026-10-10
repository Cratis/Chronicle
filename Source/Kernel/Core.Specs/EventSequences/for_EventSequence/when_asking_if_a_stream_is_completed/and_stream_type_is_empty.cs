// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_asking_if_a_stream_is_completed;

public class and_stream_type_is_empty : when_completing_a_stream_scope.given.an_event_sequence_with_closed_streams
{
    bool _result;

    async Task Establish() => await _closures.Close(new(new(EventStreamType: EventStreamType.All, EventStreamId: "month"), ClosedStreamOwner.Manual, EventSequenceNumber.First, null));

    async Task Because() => _result = await _eventSequence.IsStreamCompleted(new EventStreamType(string.Empty), "month");

    [Fact] void should_find_the_exact_all_stream_type_closure() => _result.ShouldBeTrue();
}
