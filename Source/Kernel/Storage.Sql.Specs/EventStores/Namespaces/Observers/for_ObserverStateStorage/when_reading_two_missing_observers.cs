// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage;

public class when_reading_two_missing_observers : given.an_observer_state_storage
{
    KernelObserverState _first;
    KernelObserverState _second;

    async Task Because()
    {
        _first = await _storage.Get("first");
        _second = await _storage.Get("second");
    }

    [Fact] void should_not_share_catchup_partitions() => ReferenceEquals(_first.CatchingUpPartitions, _second.CatchingUpPartitions).ShouldBeFalse();
    [Fact] void should_not_share_replaying_partitions() => ReferenceEquals(_first.ReplayingPartitions, _second.ReplayingPartitions).ShouldBeFalse();
    [Fact] void should_not_share_in_flight_partitions() => ReferenceEquals(_first.InFlightPartitions, _second.InFlightPartitions).ShouldBeFalse();
}
