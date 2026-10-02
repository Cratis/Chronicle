// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_starting_pattern_capture;

public class and_appending_to_another_sequence : given.an_event_sequence
{
    protected override EventSequenceId EventSequenceId => "another-sequence";

    async Task Because()
    {
        await AppendAnEvent();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_not_subscribe_the_event_log() => _patternCapture.DidNotReceive().EnsureSubscribedForDurableAppend(EventStore, EventStoreNamespace);
    [Fact] void should_not_schedule_a_subscription() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
