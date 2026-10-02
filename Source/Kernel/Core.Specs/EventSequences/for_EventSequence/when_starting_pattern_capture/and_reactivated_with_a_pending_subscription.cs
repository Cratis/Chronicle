// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_starting_pattern_capture;

public class and_reactivated_with_a_pending_subscription : given.an_event_sequence
{
    int _timersAfterReactivation;

    protected override Task<EventSequence> CreateEventSequence()
    {
        // Activation state is rebuilt from the durable tail, not an in-memory subscription flag.
        _stateStorage.State.SequenceNumber = EventSequenceNumber.First.Next();
        return base.CreateEventSequence();
    }

    async Task Because()
    {
        _timersAfterReactivation = _silo.TimerRegistry.NumberOfActiveTimers;
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_rearm_the_retry_on_reactivation() => _timersAfterReactivation.ShouldEqual(1);
    [Fact] void should_retry_without_another_append() => _patternCapture.Received(1).RecoverSubscription(EventStore, EventStoreNamespace);
    [Fact] void should_keep_reconciling_after_recovery() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(1);
}
