// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverStateStorage;

public class when_reading_two_missing_observers : Specification
{
    ObserverStateStorage _storage;
    ObserverState _first;
    ObserverState _second;

    void Establish() => _storage = new();

    async Task Because()
    {
        _first = await _storage.Get("first");
        _second = await _storage.Get("second");
    }

    void Destroy() => _storage.Dispose();

    [Fact] void should_not_share_catchup_partitions() => ReferenceEquals(_first.CatchingUpPartitions, _second.CatchingUpPartitions).ShouldBeFalse();
    [Fact] void should_not_share_replaying_partitions() => ReferenceEquals(_first.ReplayingPartitions, _second.ReplayingPartitions).ShouldBeFalse();
    [Fact] void should_not_share_in_flight_partitions() => ReferenceEquals(_first.InFlightPartitions, _second.InFlightPartitions).ShouldBeFalse();
}
