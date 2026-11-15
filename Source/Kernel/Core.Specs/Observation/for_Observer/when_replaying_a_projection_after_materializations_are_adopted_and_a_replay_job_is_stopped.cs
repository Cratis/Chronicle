// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_replaying_a_projection_after_materializations_are_adopted_and_a_replay_job_is_stopped : given.an_observer_with_subscription
{
    static readonly JobId _stoppedReplayJob = JobId.New();
    Exception _error;

    void Establish()
    {
        _definitionStorage.State = _definitionStorage.State with { Type = ObserverType.Projection };
        _eventStoreStorage.GenerationInitialization.GetReceipts().Returns(Task.FromResult<IReadOnlyList<GenerationInitializationReceipt>>([new GenerationInitializationReceipt(new("orders"), GenerationInitializationStep.MaterializationAdopted, "store", "evidence", new(Guid.NewGuid()), 1, DateTimeOffset.UtcNow)]));
        _jobsManager
            .GetJobs(Arg.Any<JobQuery>())
            .Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList.Create(new JobState
            {
                Id = _stoppedReplayJob,
                Status = JobStatus.Stopped,
                Request = new ReplayObserverRequest(_observerKey, ObserverType.Projection, [])
            })));
        _jobsManager.Resume(_stoppedReplayJob).Returns(true);
    }

    async Task Because() => _error = await Catch.Exception(_observer.Replay);

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<GenerationInitializationReplayNotSupported>();
    [Fact] async Task should_not_resume_the_stopped_replay_job() => await _jobsManager.DidNotReceive().Resume(Arg.Any<JobId>());
}
