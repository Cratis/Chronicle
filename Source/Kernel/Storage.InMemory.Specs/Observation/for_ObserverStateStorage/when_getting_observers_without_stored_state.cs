// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverStateStorage;

/// <summary>
/// An observer owns and mutates its partition sets, so two observers without stored state must not share them
/// (Cratis/Chronicle#4558).
/// </summary>
public class when_getting_observers_without_stored_state : Specification
{
    static readonly Key _partition = "partition";

    ObserverStateStorage _storage;
    ObserverState _first;
    ObserverState _second;

    void Establish() => _storage = new();

    async Task Because()
    {
        _first = await _storage.Get("first");
        _second = await _storage.Get("second");
        _first.CatchingUpPartitions.Add(_partition);
        _first.ReplayingPartitions.Add(_partition);
        _first.InFlightPartitions.Add(_partition);
    }

    [Fact] void should_not_share_catching_up_partitions() => _second.CatchingUpPartitions.ShouldNotContain(_partition);
    [Fact] void should_not_share_replaying_partitions() => _second.ReplayingPartitions.ShouldNotContain(_partition);
    [Fact] void should_not_share_in_flight_partitions() => _second.InFlightPartitions.ShouldNotContain(_partition);
    [Fact] void should_leave_the_empty_state_untouched() => ObserverState.Empty.CatchingUpPartitions.ShouldNotContain(_partition);

    void Destroy() => _storage.Dispose();
}
