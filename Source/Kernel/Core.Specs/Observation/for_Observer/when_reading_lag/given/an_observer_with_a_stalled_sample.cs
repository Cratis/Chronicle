// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.for_CatchUpAdmission.given;

namespace Cratis.Chronicle.Observation.for_Observer.when_reading_lag.given;

public abstract class an_observer_with_a_stalled_sample : when_watchdog_runs.given.an_observer_behind_on_a_relevant_event
{
    readonly ManualTimeProvider _clock = new();

    protected an_observer_with_a_stalled_sample() => _timeProvider = _clock;

    protected override Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 0, StallWindow = TimeSpan.FromSeconds(30) };

    async Task Establish()
    {
        await _observer.RunWatchdogAsync();
        _clock.Advance(_observersConfig.StallWindow);
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)21UL);
        await _observer.RunWatchdogAsync();
        (await _observer.GetInformation()).LagSample.Status.ShouldEqual(ObserverLagStatus.Stalled);
    }
}
