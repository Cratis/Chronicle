// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Projections.for_ProjectionsManager.given;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition.given;

public class a_recovered_global_partition_replay : a_partition_replay_job
{
    void Establish()
    {
        _stateStorage.State.Request = _request;
        _stateStorage.State.Status = JobStatus.Stopped;
        var identifier = _request.ObserverKey.ObserverId.Value;
        _eventStore.Projections.Has(identifier).Returns(true);
        _eventStore.Projections.Get(identifier).Returns(
            a_projections_manager_grain.CreateDefinition(identifier, "read-model") with { Scope = ProjectionScope.Both });
    }
}
