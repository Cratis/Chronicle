// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Projections.for_ProjectionsManager.given;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.given;

public class a_recovered_global_replay : a_replay_observer_job
{
    protected virtual ProjectionScope Scope => ProjectionScope.Global;

    void Establish()
    {
        _stateStorage.State.Request = _request;
        _stateStorage.State.Status = JobStatus.Stopped;
        _observer.GetState().Returns(ObserverState.Empty with { RunningState = ObserverRunningState.Replaying });
        var identifier = _request.ObserverKey.ObserverId.Value;
        _eventStoreStorage.Projections.Has(identifier).Returns(true);
        _eventStoreStorage.Projections.Get(identifier).Returns(
            a_projections_manager_grain.CreateDefinition(identifier, "read-model") with { Scope = Scope });
    }
}
