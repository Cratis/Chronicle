// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_replaying : given.an_observer_with_subscription
{
    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        _storageStats.ResetCounts();
    }

    Task Because() => _observer.Replay();

    [Fact] void should_set_running_state_to_replaying() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Replaying);
    [Fact] void should_write_state_once() => _storageStats.Writes.ShouldEqual(1);
}
