// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_replaying_partition_and_observer_is_not_replayable : given.an_observer_with_subscription
{
    Key _partition;

    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        _definitionStorage.State = _definitionStorage.State with { IsReplayable = false };
        _partition = "some-partition";
        _storageStats.ResetCounts();
    }

    Task Because() => _observer.ReplayPartitionTo(_partition, EventSequenceNumber.Max);

    [Fact] void should_not_change_replaying_partitions() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
}
