// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_OptimisticConcurrencyStrategy.when_getting_a_scope;

/// <summary>
/// The kernel validates an append by reading the tail through the scope's own narrowing, so the expected
/// sequence number has to be read the same way. Reading the tail of the whole event source while the kernel
/// compares against the tail of one stream within it means the two are answering different questions — and the
/// mismatch is silent, showing up as conflicts reported between appends the scope says cannot conflict, or as
/// real conflicts never reported at all.
/// </summary>
public class and_it_narrows_by_stream_metadata : given.an_optimistic_concurrency_strategy
{
    static readonly EventStreamType _eventStreamType = new("Onboarding");
    static readonly EventStreamId _eventStreamId = new("2026-08");
    static readonly EventSourceType _eventSourceType = new("Customer");

    ConcurrencyScope _result;

    async Task Because() => _result = await _strategy.GetScope(_eventSourceId, _eventStreamType, _eventStreamId, _eventSourceType);

    [Fact] void should_read_the_tail_with_the_same_narrowing_it_scopes_by() => _eventSequence.Received(1).GetTailSequenceNumber(
        _eventSourceId,
        _eventSourceType,
        _eventStreamType,
        _eventStreamId,
        Arg.Any<IEnumerable<EventType>?>());

    [Fact] void should_carry_the_tail_it_read() => _result.SequenceNumber.ShouldEqual(_tail);
    [Fact] void should_scope_to_the_event_source() => _result.EventSourceId.ShouldEqual(_eventSourceId);
    [Fact] void should_scope_to_the_event_stream_type() => _result.EventStreamType.ShouldEqual(_eventStreamType);
    [Fact] void should_scope_to_the_event_stream_id() => _result.EventStreamId.ShouldEqual(_eventStreamId);
    [Fact] void should_scope_to_the_event_source_type() => _result.EventSourceType.ShouldEqual(_eventSourceType);
}
