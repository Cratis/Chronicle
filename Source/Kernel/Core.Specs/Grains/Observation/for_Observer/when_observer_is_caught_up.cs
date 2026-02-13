// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Properties;
namespace Cratis.Chronicle.Grains.Observation.for_Observer;

public class when_observer_is_caught_up : given.an_observer_with_subscription
{
    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [], SiloAddress.Zero);
        _stateStorage.State.CatchingUpPartitions.Add(new Key("partition1", ArrayIndexers.NoIndexers));
        _stateStorage.State.CatchingUpPartitions.Add(new Key("partition2", ArrayIndexers.NoIndexers));
        _storageStats.ResetCounts();
    }

    Task Because() => _observer.CaughtUp(42L);

    [Fact] void should_clear_catching_up_observers() => _stateStorage.State.CatchingUpPartitions.ShouldBeEmpty();
    [Fact] void should_write_state() => _storageStats.Writes.ShouldBeGreaterThan(0);
}
