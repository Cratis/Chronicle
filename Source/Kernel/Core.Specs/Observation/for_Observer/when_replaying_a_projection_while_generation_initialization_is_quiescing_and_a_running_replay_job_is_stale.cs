// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_replaying_a_projection_while_generation_initialization_is_quiescing_and_a_running_replay_job_is_stale : given.an_observer_with_subscription
{
    readonly given.RecordingObserverLogger _recordedLogs = new();
    Exception _error;

    protected override ILogger<Observer> CreateLogger() => _recordedLogs;

    void Establish()
    {
        _definitionStorage.State = _definitionStorage.State with { Type = ObserverType.Projection };
        _eventStoreStorage.GenerationInitialization.GetReceipts().Returns(Task.FromResult<IReadOnlyList<GenerationInitializationReceipt>>([new GenerationInitializationReceipt(new("orders"), GenerationInitializationStep.FloorVerified, "store", "evidence", new(Guid.NewGuid()), 1, DateTimeOffset.UtcNow)]));
        _jobsManager
            .GetJobs(Arg.Any<JobQuery>())
            .Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList.Create(new JobState
            {
                Id = JobId.New(),
                Status = JobStatus.Running,
                Request = new ReplayObserverRequest(_observerKey, ObserverType.Projection, []),
                Progress = new() { TotalSteps = 2, SuccessfulSteps = 2 },
                StatusChanges = [new JobStatusChanged { Status = JobStatus.Running, Occurred = DateTimeOffset.UtcNow - JobLiveness.StaleAfter - TimeSpan.FromMinutes(1) }]
            })));
    }

    async Task Because() => _error = await Catch.Exception(_observer.Replay);

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<GenerationInitializationReplayNotSupported>();
    [Fact] void should_explain_the_refusal() => _error.Message.ShouldEqual(GenerationInitializationReplayNotSupported.StartRefusedReason);
    [Fact] void should_not_record_a_replay() => _stateStorage.State.IsReplaying.ShouldBeFalse();
    [Fact] async Task should_not_start_a_job() => await _jobsManager.DidNotReceive().Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
    [Fact] async Task should_leave_the_replay_state() => ((await _observer.GetCurrentState()) is Replay).ShouldBeFalse();
    [Fact] void should_not_be_replaying() => _stateStorage.State.RunningState.ShouldNotEqual(ObserverRunningState.Replaying);
    [Fact] void should_release_the_refused_replay() => _recordedLogs.Warnings.Any(_ => _.StartsWith("Released a refused replay", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_record_the_refusal() => _recordedLogs.Warnings.Any(_ => _.Contains(GenerationInitializationReplayNotSupported.StartRefusedReason, StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] async Task should_not_reset_the_handled_counts() => await _observerHandledCountsStorage.DidNotReceive().RemoveAllFor(Arg.Any<ObserverId>());
}
