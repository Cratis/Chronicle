// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.given;

namespace Cratis.Chronicle.Observation.for_Observer.when_changing_running_state;

public class and_the_observer_disconnects : given.an_observer
{
    readonly DateTimeOffset _now = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    DeliveryClock _clock;

    async Task Establish()
    {
        _clock = new();
        _clock.Advance(_now - DateTimeOffset.UnixEpoch - TimeSpan.FromSeconds(1));
        _timeProvider = _clock;
        await Crash();
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [], SiloAddress.Zero);
    }

    async Task Because()
    {
        _clock.Advance(TimeSpan.FromSeconds(1));
        await _observer.Unsubscribe();
    }

    [Fact] void should_be_disconnected() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Disconnected);
    [Fact] void should_stamp_the_transition_time() => _stateStorage.State.RunningStateChangedAt.ShouldEqual(_now);
}
