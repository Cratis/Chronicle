// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_starting_pattern_capture;

public class after_the_first_durable_append : given.an_event_sequence
{
    AppendResult _result;
    bool _subscribedBeforeTheAppendReturned;
    bool _eventWasDurableWhenSubscribing;

    void Establish() => _patternCapture.RecoverSubscription(EventStore, EventStoreNamespace).Returns(_ =>
    {
        _eventWasDurableWhenSubscribing = _appendedSequenceNumber.IsActualValue;
        return Task.CompletedTask;
    });

    async Task Because()
    {
        _result = await AppendAnEvent();
        _subscribedBeforeTheAppendReturned = _patternCapture.ReceivedCalls().Any();
        await _silo.TimerRegistry.FireAllAsync();
        await AppendAnEvent();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_wait_for_subscription_in_the_append() => _subscribedBeforeTheAppendReturned.ShouldBeFalse();
    [Fact] void should_subscribe_after_the_event_is_durable() => _eventWasDurableWhenSubscribing.ShouldBeTrue();
    [Fact] void should_reconcile_after_each_tick() => _patternCapture.Received(2).RecoverSubscription(EventStore, EventStoreNamespace);
    [Fact] void should_keep_the_reconciliation_timer() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(1);
}
