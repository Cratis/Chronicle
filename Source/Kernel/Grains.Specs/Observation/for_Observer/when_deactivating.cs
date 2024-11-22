// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Grains.Observation.for_Observer;

public class when_deactivating : given.an_observer
{
    async Task Because() => await observer.OnDeactivateAsync(default, default);

    [Fact] void should_set_running_state_to_disconnected() => state_storage.State.RunningState.ShouldEqual(ObserverRunningState.Disconnected);
    [Fact] void should_write_state_once() => storage_stats.Writes.ShouldEqual(1);
}