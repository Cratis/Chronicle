// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_routing_is_waiting_for_event_lookup_retry : given.an_observer_waiting_for_event_lookup_retry
{
    int _lookupsAfterFirstTick;

    async Task Because()
    {
        await _observer.RunWatchdogAsync();
        _lookupsAfterFirstTick = LookupCount();
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_retry_once_on_the_first_tick() => _lookupsAfterFirstTick.ShouldEqual(1);
    [Fact] void should_retry_once_on_the_second_tick() => LookupCount().ShouldEqual(2);
    [Fact] async Task should_stay_in_routing() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Routing>();
    [Fact] void should_not_deliver_live_events() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Unknown);
    [Fact] void should_leave_the_position_unchanged() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual(_nextSequenceNumber);
    [Fact] void should_not_start_catch_up() => ShouldNotHaveStartedCatchup();
    [Fact] void should_not_subscribe_to_live_events() => ShouldNotHaveResubscribed();
}
