// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_starting_pattern_capture;

public class and_subscription_fails : given.an_event_sequence
{
    AppendResult _result;
    int _timersAfterFailure;

    void Establish() => _patternCapture.Subscribe(EventStore, EventStoreNamespace).Returns(
        _ => Task.FromException(new given.SimulatedStorageError()),
        _ => Task.CompletedTask);

    async Task Because()
    {
        _result = await AppendAnEvent();
        await _silo.TimerRegistry.FireAllAsync();
        _timersAfterFailure = _silo.TimerRegistry.NumberOfActiveTimers;
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_not_fail_the_durable_append() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_retry_timer() => _timersAfterFailure.ShouldEqual(1);
    [Fact] void should_retry_without_another_append() => _patternCapture.Received(2).Subscribe(EventStore, EventStoreNamespace);
    [Fact] void should_release_the_timer_after_recovery() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
