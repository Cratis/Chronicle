// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_subscribing_with_non_replayable_args : given.an_observer_with_subscription
{
    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero, null, false);
        _storageStats.ResetCounts();
    }

    Task Because() => _observer.Replay();

    [Fact] void should_set_is_replayable_to_false() => _definitionStorage.State.IsReplayable.ShouldBeFalse();
    [Fact] void should_not_change_running_state_on_replay() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_not_write_state_on_replay() => _storageStats.Writes.ShouldEqual(0);
}
