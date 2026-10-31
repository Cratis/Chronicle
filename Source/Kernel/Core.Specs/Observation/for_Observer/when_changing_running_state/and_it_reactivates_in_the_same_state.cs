// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_changing_running_state;

public class and_it_reactivates_in_the_same_state : given.an_observer
{
    readonly DateTimeOffset _previous = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { RunningState = ObserverRunningState.Disconnected };
        _persistedRunningStateChangedAt = _previous;
    }

    Task Because() => Crash();

    [Fact] void should_stay_disconnected() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Disconnected);
    [Fact] void should_preserve_when_it_disconnected() => _persistedRunningStateChangedAt.ShouldEqual(_previous);
}
