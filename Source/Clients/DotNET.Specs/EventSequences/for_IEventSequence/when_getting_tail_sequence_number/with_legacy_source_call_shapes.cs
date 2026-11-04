// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_IEventSequence.when_getting_tail_sequence_number;

public class with_legacy_source_call_shapes : given.a_legacy_event_sequence
{
    EventSequenceNumber _defaultResult;
    EventSequenceNumber _filteredResult;

    async Task Because()
    {
        _defaultResult = await _sequence.GetTailSequenceNumber(default).WaitAsync(TimeSpan.FromSeconds(10));
        _filteredResult = await _sequence.GetTailSequenceNumber("source", "source-type", "stream-type", "stream", [new EventType("type", EventTypeGeneration.First)]).WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_resolve_the_default_literal_to_the_old_method() => _defaultResult.ShouldEqual((EventSequenceNumber)5460UL);
    [Fact] void should_resolve_all_five_filters_to_the_old_method() => _filteredResult.ShouldEqual((EventSequenceNumber)5460UL);
    [Fact] void should_execute_both_calls() => _implementation.TailCalls.ShouldEqual(2);
}
