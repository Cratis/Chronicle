// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverStateGrainStorageProvider;

/// <summary>
/// Every storage implementation (MongoDB, SQL, in-memory) answers an observer with no stored state with the shared
/// <see cref="ObserverState.Empty"/> instance. Two brand-new observers read through the provider must still own their
/// partition sets independently: a reactor registering a partition as catching up must not make an unrelated
/// projection drop live events for that partition (Cratis/Chronicle#4558).
/// </summary>
public class when_reading_state_for_two_observers_without_stored_state : given.the_provider
{
    static readonly Key _partition = "partition";

    GrainId _reactorGrainId;
    GrainId _projectionGrainId;
    IGrainState<ObserverState> _reactorState;
    IGrainState<ObserverState> _projectionState;

    void Establish()
    {
        _reactorGrainId = GrainId.Create("type", new ObserverKey("reactor", "store", "Default", "event-log").ToString());
        _projectionGrainId = GrainId.Create("type", new ObserverKey("projection", "store", "Default", "event-log").ToString());
        _reactorState = new GrainState<ObserverState> { State = new() };
        _projectionState = new GrainState<ObserverState> { State = new() };

        observerStateStorage.Get(Arg.Any<ObserverId>()).Returns(Task.FromResult(ObserverState.Empty));
    }

    async Task Because()
    {
        await provider.ReadStateAsync("name", _reactorGrainId, _reactorState);
        await provider.ReadStateAsync("name", _projectionGrainId, _projectionState);
        _reactorState.State.CatchingUpPartitions.Add(_partition);
        _reactorState.State.ReplayingPartitions.Add(_partition);
        _reactorState.State.InFlightPartitions.Add(_partition);
    }

    [Fact] void should_not_mark_the_partition_as_catching_up_for_the_other_observer() => _projectionState.State.CatchingUpPartitions.ShouldNotContain(_partition);
    [Fact] void should_not_mark_the_partition_as_replaying_for_the_other_observer() => _projectionState.State.ReplayingPartitions.ShouldNotContain(_partition);
    [Fact] void should_not_mark_the_partition_as_in_flight_for_the_other_observer() => _projectionState.State.InFlightPartitions.ShouldNotContain(_partition);
    [Fact] void should_leave_the_empty_state_untouched() => ObserverState.Empty.CatchingUpPartitions.ShouldNotContain(_partition);

    void Destroy()
    {
        // On the defective implementation these sets are ObserverState.Empty's own; do not leak into other specs.
        _reactorState.State.CatchingUpPartitions.Remove(_partition);
        _reactorState.State.ReplayingPartitions.Remove(_partition);
        _reactorState.State.InFlightPartitions.Remove(_partition);
    }
}
