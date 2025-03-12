// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Grains.Observation.for_Observer;

public class when_deactivating : given.an_observer
{
    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [], SiloAddress.Zero);
        _storageStats.ResetCounts();
    }

    async Task Because() => await _observer.OnDeactivateAsync(default, default);

    [Fact] void should_set_running_state_to_disconnected() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Disconnected);
    [Fact] void should_write_state_once() => _storageStats.Writes.ShouldEqual(1);
}
