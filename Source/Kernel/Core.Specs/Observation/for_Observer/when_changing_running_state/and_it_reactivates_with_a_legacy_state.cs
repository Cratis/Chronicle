// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_changing_running_state;

public class and_it_reactivates_with_a_legacy_state : given.an_observer
{
    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { RunningState = ObserverRunningState.Disconnected };
        _persistedRunningStateChangedAt = null;
    }

    Task Because() => Crash();

    [Fact] void should_stay_disconnected() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Disconnected);
    [Fact] void should_leave_the_timestamp_unknown() => _persistedRunningStateChangedAt.ShouldBeNull();
}
