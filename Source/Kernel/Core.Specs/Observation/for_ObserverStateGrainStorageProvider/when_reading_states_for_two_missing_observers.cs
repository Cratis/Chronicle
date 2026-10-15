// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverStateGrainStorageProvider;

public class when_reading_states_for_two_missing_observers : given.the_provider
{
    readonly IGrainState<ObserverState> _first = new GrainState<ObserverState> { State = new() };
    readonly IGrainState<ObserverState> _second = new GrainState<ObserverState> { State = new() };
    readonly Key _partition = "first-observer-partition";

    void Establish() => observerStateStorage.Get(Arg.Any<ObserverId>()).Returns(ObserverState.Empty);

    async Task Because()
    {
        await provider.ReadStateAsync("name", GrainId.Create("type", "first-observer"), _first);
        _first.State.ReplayingPartitions.Add(_partition);
        _first.State.CatchingUpPartitions.Add(_partition);
        _first.State.InFlightPartitions.Add(_partition);
        await provider.ReadStateAsync("name", GrainId.Create("type", "second-observer"), _second);
    }

    [Fact] void should_not_share_replaying_partitions() => ReferenceEquals(_first.State.ReplayingPartitions, _second.State.ReplayingPartitions).ShouldBeFalse();
    [Fact] void should_not_share_catching_up_partitions() => ReferenceEquals(_first.State.CatchingUpPartitions, _second.State.CatchingUpPartitions).ShouldBeFalse();
    [Fact] void should_not_share_in_flight_partitions() => ReferenceEquals(_first.State.InFlightPartitions, _second.State.InFlightPartitions).ShouldBeFalse();
    [Fact] void should_start_the_second_observer_without_replaying_partitions() => _second.State.ReplayingPartitions.ShouldBeEmpty();
    [Fact] void should_start_the_second_observer_without_catching_up_partitions() => _second.State.CatchingUpPartitions.ShouldBeEmpty();
    [Fact] void should_start_the_second_observer_without_in_flight_partitions() => _second.State.InFlightPartitions.ShouldBeEmpty();
    [Fact] void should_not_reuse_the_sentinel_replaying_partitions() => ReferenceEquals(_first.State.ReplayingPartitions, ObserverState.Empty.ReplayingPartitions).ShouldBeFalse();
    [Fact] void should_not_reuse_the_sentinel_catching_up_partitions() => ReferenceEquals(_first.State.CatchingUpPartitions, ObserverState.Empty.CatchingUpPartitions).ShouldBeFalse();
    [Fact] void should_not_reuse_the_sentinel_in_flight_partitions() => ReferenceEquals(_first.State.InFlightPartitions, ObserverState.Empty.InFlightPartitions).ShouldBeFalse();
}
