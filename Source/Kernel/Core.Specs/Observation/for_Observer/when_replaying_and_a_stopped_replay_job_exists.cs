// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_replaying_and_a_stopped_replay_job_exists : given.an_observer_with_subscription
{
    static readonly JobId _stoppedReplayJob = JobId.New();
    JobId _result = JobId.NotSet;

    void Establish()
    {
        _jobsManager
            .GetJobs(Arg.Any<JobQuery>())
            .Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList.Create(new JobState
            {
                Id = _stoppedReplayJob,
                Status = JobStatus.Stopped,
                Request = new ReplayObserverRequest(_observerKey, ObserverType.Reactor, [])
            })));
        _jobsManager.Resume(_stoppedReplayJob).Returns(true);
    }

    async Task Because() => _result = await _observer.Replay();

    [Fact] void should_return_the_resumed_replay_job() => _result.ShouldEqual(_stoppedReplayJob);
    [Fact] void should_be_replaying() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Replaying);
    [Fact] async Task should_resume_the_stopped_replay_job() => await _jobsManager.Received(1).Resume(_stoppedReplayJob);

    [Fact]
    async Task should_not_start_another_replay() =>
        await _jobsManager.DidNotReceive().Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
}
