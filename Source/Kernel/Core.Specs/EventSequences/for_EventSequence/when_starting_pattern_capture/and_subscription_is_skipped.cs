// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_starting_pattern_capture;

public class and_subscription_is_skipped : given.an_event_sequence
{
    int _timersAfterSkipping;

    void Establish() => _patternCapture.RecoverSubscription(EventStore, EventStoreNamespace).Returns(Task.CompletedTask);

    async Task Because()
    {
        await AppendAnEvent();
        await _silo.TimerRegistry.FireAllAsync();
        _timersAfterSkipping = _silo.TimerRegistry.NumberOfActiveTimers;
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_keep_retrying_a_skipped_subscription() => _timersAfterSkipping.ShouldEqual(1);
    [Fact] void should_retry_without_another_append() => _patternCapture.Received(2).RecoverSubscription(EventStore, EventStoreNamespace);
    [Fact] void should_keep_reconciling() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(1);
}
