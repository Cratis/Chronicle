// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_resuming;

public class and_reducer_attachment_fails : given.a_replay_observer_job
{
    IHandleEventsForPartition _step;

    void Establish()
    {
        _stateStorage.State.Request = _request with { ObserverType = ObserverType.Reducer };
        _stateStorage.State.Status = JobStatus.Stopped;
        _stateStorage.State.StatusChanges.Add(new() { Status = JobStatus.StartingSteps });
        _stateStorage.State.Progress.TotalSteps = 1;
        _stateStorage.State.Progress.StoppedSteps = 1;
        _observer.IsSubscribed().Returns(true);
        _step = Substitute.For<IHandleEventsForPartition>();
        _silo.AddProbe(_ => _step);
        var stepState = new JobStepState
        {
            Id = new(_jobId, JobStepId.New()),
            Type = typeof(IHandleEventsForPartition),
            Status = JobStepStatus.Stopped
        };
        _jobStepStorage.GetForJob(Arg.Any<JobId>(), Arg.Any<JobStepStatus[]>())
            .Returns(Task.FromResult(Catch<IImmutableList<JobStepState>>.Success(ImmutableList.Create(stepState))));
        _replayServiceClient.ResumeReplayFor(Arg.Any<ObserverDetails>())
            .Returns(Task.FromException(new ReplayInitializationFailed(ICanHandleReplayForObserver.Error.CouldNotGetReplayContext)));
    }

    async Task Because()
    {
        await _job.Resume();
        await _job.Resume();
    }

    [Fact] void should_not_start_unattached_steps() => _step.DidNotReceiveWithAnyArgs().Start(default);
    [Fact] void should_remain_stopped_for_a_safe_retry() => _stateStorage.State.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_validate_attachment_again_on_retry() => _replayServiceClient.Received(2).ResumeReplayFor(Arg.Any<ObserverDetails>());
}
