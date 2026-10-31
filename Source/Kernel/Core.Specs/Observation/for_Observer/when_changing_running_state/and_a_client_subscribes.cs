// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.given;

namespace Cratis.Chronicle.Observation.for_Observer.when_changing_running_state;

public class and_a_client_subscribes : given.an_observer
{
    readonly DateTimeOffset _previous = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    readonly DateTimeOffset _now = new(2026, 1, 3, 3, 4, 5, TimeSpan.Zero);

    async Task Establish()
    {
        var clock = new DeliveryClock();
        clock.Advance(_now - DateTimeOffset.UnixEpoch);
        _timeProvider = clock;
        _stateStorage.State = _stateStorage.State with { RunningState = ObserverRunningState.Disconnected, RunningStateChangedAt = _previous };
        await Crash();
    }

    Task Because() => _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);

    [Fact] void should_be_active() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_renew_the_timestamp() => _stateStorage.State.RunningStateChangedAt.ShouldNotEqual(_previous);
    [Fact] void should_stamp_the_transition_time() => _stateStorage.State.RunningStateChangedAt.ShouldEqual(_now);
}
