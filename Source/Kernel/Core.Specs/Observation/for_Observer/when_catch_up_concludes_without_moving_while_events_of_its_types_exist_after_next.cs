// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_catch_up_concludes_without_moving_while_events_of_its_types_exist_after_next : given.an_observer_with_subscription
{
    int _timersBefore;

    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        await _observer.CaughtUp(JobId.New(), 42L);
        _eventSequence.GetTailSequenceNumberForEventTypes(Arg.Any<IEnumerable<EventType>>()).Returns((EventSequenceNumber)100L);
        _observer.SetCatchupRecoveryAttemptsForTesting(3);
        _timersBefore = _silo.TimerRegistry.NumberOfActiveTimers;
    }

    async Task Because() => await _observer.CaughtUp(JobId.New(), 42L);

    [Fact] async Task should_not_back_off() => (await _observer.IsCatchUpBackedOff()).ShouldBeFalse();
    [Fact] void should_schedule_a_retry() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(_timersBefore + 1);
    [Fact] void should_preserve_the_recovery_attempts() => _observer.CatchupRecoveryAttempts.ShouldEqual(3);
    [Fact] void should_probe_the_subscription_event_types() => _eventSequence.Received(1).GetTailSequenceNumberForEventTypes(Arg.Is<IEnumerable<EventType>>(types => types.SequenceEqual(new[] { EventType.Unknown })));
}
