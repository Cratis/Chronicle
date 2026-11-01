// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_replaying_a_projection_while_generation_initialization_is_quiescing_and_a_running_replay_job_is_live : given.an_observer_with_subscription
{
    static readonly JobId _runningReplayJob = JobId.New();
    JobId _result = JobId.NotSet;

    void Establish()
    {
        _definitionStorage.State = _definitionStorage.State with { Type = ObserverType.Projection };
        _eventStoreStorage.GenerationInitialization.GetReceipts().Returns(Task.FromResult<IReadOnlyList<GenerationInitializationReceipt>>([new GenerationInitializationReceipt(new("orders"), GenerationInitializationStep.FloorVerified, "store", "evidence", new(Guid.NewGuid()), 1, DateTimeOffset.UtcNow)]));
        _jobsManager
            .GetJobs(Arg.Any<JobQuery>())
            .Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList.Create(new JobState
            {
                Id = _runningReplayJob,
                Status = JobStatus.Running,
                Request = new ReplayObserverRequest(_observerKey, ObserverType.Projection, []),
                Progress = new() { TotalSteps = 2, SuccessfulSteps = 1 },
                StatusChanges = [new JobStatusChanged { Status = JobStatus.Running, Occurred = DateTimeOffset.UtcNow }]
            })));
    }

    async Task Because() => _result = await _observer.Replay();

    [Fact] void should_return_the_running_replay_job() => _result.ShouldEqual(_runningReplayJob);
    [Fact] async Task should_not_start_another_replay() => await _jobsManager.DidNotReceive().Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
}
