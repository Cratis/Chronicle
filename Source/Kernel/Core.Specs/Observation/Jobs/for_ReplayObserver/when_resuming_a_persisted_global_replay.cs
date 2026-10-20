// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Projections.for_ProjectionsManager.given;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver;

public class when_resuming_a_persisted_global_replay : given.a_replay_observer_job
{
    Exception _error;

    void Establish()
    {
        _stateStorage.State.Request = _request;
        _observer.GetState().Returns(ObserverState.Empty with { RunningState = ObserverRunningState.Replaying });
        var identifier = _request.ObserverKey.ObserverId.Value;
        _eventStoreStorage.Projections.Has(identifier).Returns(true);
        _eventStoreStorage.Projections.Get(identifier).Returns(
            a_projections_manager_grain.CreateDefinition(identifier, "read-model") with { Scope = ProjectionScope.Global });
    }

    async Task Because() => _error = await Catch.Exception(_job.ResumeForTesting);

    [Fact] void should_refuse_before_resuming_steps() => _error.ShouldBeOfExactType<GlobalProjectionReplayNotSupported>();
    [Fact] async Task should_not_resume_the_replay_on_silos() => await _replayServiceClient.DidNotReceive().ResumeReplayFor(Arg.Any<ObserverDetails>());
}
