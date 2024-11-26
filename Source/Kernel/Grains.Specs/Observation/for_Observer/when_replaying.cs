// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
namespace Cratis.Chronicle.Grains.Observation.for_Observer;

public class when_replaying : given.an_observer_with_subscription
{
    async Task Establish()
    {
        await observer.Subscribe<NullObserverSubscriber>(ObserverType.Client, [], SiloAddress.Zero);
        storage_stats.ResetCounts();
    }

    Task Because() => observer.Replay();

    [Fact] void should_set_running_state_to_replaying() => state_storage.State.RunningState.ShouldEqual(ObserverRunningState.Replaying);
    [Fact] void should_write_state_once() => storage_stats.Writes.ShouldEqual(1);
}
