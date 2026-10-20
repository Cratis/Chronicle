// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Projections.for_ProjectionsManager.given;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_replaying_a_global_projection_partition : given.an_observer_with_subscription
{
    Exception _error;

    void Establish()
    {
        _definitionStorage.State = _definitionStorage.State with { Type = ObserverType.Projection };
        _stateStorage.State = _stateStorage.State with { LastHandledEventSequenceNumber = 42UL };
        _eventStoreStorage.Projections.Has(_observerId.Value).Returns(true);
        _eventStoreStorage.Projections.Get(_observerId.Value).Returns(
            a_projections_manager_grain.CreateDefinition(_observerId.Value, "read-model") with { Scope = ProjectionScope.Global });
    }

    async Task Because() => _error = await Catch.Exception(() => _observer.ReplayPartition("partition"));

    [Fact] void should_refuse_partition_replay() => _error.ShouldBeOfExactType<GlobalProjectionReplayNotSupported>();
    [Fact] void should_preserve_progress() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_not_write_observer_state() => _storageStats.Writes.ShouldEqual(0);
    [Fact] async Task should_not_start_a_job() => await _jobsManager.DidNotReceive().Start<IReplayObserverPartition, ReplayObserverPartitionRequest>(Arg.Any<ReplayObserverPartitionRequest>());
}
