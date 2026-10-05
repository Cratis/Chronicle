// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_OptimisticConcurrencyStrategy;

public class when_getting_scope_for_a_stream_without_the_event_source_id : given_an_event_sequence
{
    ConcurrencyScope _scope;

    async Task Because() => _scope = await _strategy.GetScope(
        ConcurrencyDimensions.EventStreamType | ConcurrencyDimensions.EventStreamId,
        _eventSourceId,
        "Items",
        "stream-1",
        "Cart");

    [Fact] void should_leave_the_event_source_id_out() => _scope.EventSourceId.ShouldBeNull();
    [Fact] void should_leave_the_event_source_type_out() => _scope.EventSourceType.ShouldBeNull();
    [Fact] void should_keep_the_stream_type() => _scope.EventStreamType!.Value.ShouldEqual("Items");
    [Fact] void should_keep_the_stream_id() => _scope.EventStreamId!.Value.ShouldEqual("stream-1");
    [Fact] void should_read_the_tail_with_the_same_narrowing() => _eventSequence.Received(1).GetTailSequenceNumber(
        null,
        null,
        Arg.Is<EventStreamType?>(_ => _!.Value == "Items"),
        Arg.Is<EventStreamId?>(_ => _!.Value == "stream-1"),
        Arg.Any<IEnumerable<EventType>?>());
    [Fact] void should_carry_the_tail_as_expected_sequence_number() => _scope.SequenceNumber.Value.ShouldEqual(42UL);
}
