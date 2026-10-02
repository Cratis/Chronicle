// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverStateGrainStorageProvider;

public class when_reading_a_shared_empty_state : given.the_provider
{
    ObserverState _empty;
    GrainState<ObserverState> _first;
    GrainState<ObserverState> _second;

    void Establish()
    {
        // The providers return ObserverState.Empty for absent rows. Use a private sentinel here so
        // a regression cannot corrupt other specs by mutating the process-wide one.
        _empty = new();
        observerStateStorage.Get(Arg.Any<ObserverId>()).Returns(_empty);
        _first = new() { State = new() };
        _second = new() { State = new() };
    }

    async Task Because()
    {
        await provider.ReadStateAsync("state", GrainId.Create("observer", "first#store#tenant#event-log"), _first);
        await provider.ReadStateAsync("state", GrainId.Create("observer", "second#store#tenant#event-log"), _second);
        _first.State.CatchingUpPartitions.Add((Key)"customer");
        _first.State.ReplayingPartitions.Add((Key)"customer");
        _first.State.InFlightPartitions.Add((Key)"customer");
        _second.State.CatchingUpPartitions.Clear();
    }

    [Fact] void should_not_clear_another_observers_catchup() => _first.State.CatchingUpPartitions.ShouldContainOnly((Key)"customer");
    [Fact] void should_not_share_replaying_partitions() => _second.State.ReplayingPartitions.ShouldBeEmpty();
    [Fact] void should_not_share_in_flight_partitions() => _second.State.InFlightPartitions.ShouldBeEmpty();
    [Fact] void should_not_mutate_the_empty_catchup_set() => _empty.CatchingUpPartitions.ShouldBeEmpty();
    [Fact] void should_not_mutate_the_empty_replay_set() => _empty.ReplayingPartitions.ShouldBeEmpty();
    [Fact] void should_not_mutate_the_empty_in_flight_set() => _empty.InFlightPartitions.ShouldBeEmpty();
}
