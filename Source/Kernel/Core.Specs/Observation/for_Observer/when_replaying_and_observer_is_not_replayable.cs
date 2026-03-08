// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_replaying_and_observer_is_not_replayable : given.an_observer_with_subscription
{
    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        _definitionStorage.State = _definitionStorage.State with { IsReplayable = false };
        _storageStats.ResetCounts();
    }

    Task Because() => _observer.Replay();

    [Fact] void should_not_change_running_state() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
}
