// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reconciling_pattern_capture;

public class and_the_sequence_is_not_the_event_log : given.an_event_sequence_with_pattern_capture
{
    protected override EventSequenceId SequenceId => "another-sequence";

    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] async Task should_not_subscribe_capture() => await _patternCapture.DidNotReceive().Subscribe(EventStore, EventStoreNamespace);
    [Fact] void should_not_schedule_capture_reconciliation() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
