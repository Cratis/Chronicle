// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// A replay request finding a stopped replay job resumes it and answers with that job, as the job that is replaying
/// (Cratis/Chronicle#4514).
/// </summary>
public class when_replaying_and_a_stopped_replay_job_exists : given.an_observer_with_subscription
{
    static readonly JobId _stoppedJob = JobId.New();

    IImmutableList<JobState> _unfinishedJobs;
    JobId _result;

    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);

        var request = new ReplayObserverRequest(_observerKey, ObserverType.Reactor, [EventType.Unknown]);
        _unfinishedJobs = ImmutableList.Create(new JobState { Id = _stoppedJob, Status = JobStatus.Stopped, Request = request });
        _jobsManager
            .GetJobs(Arg.Any<JobQuery>())
            .Returns(_ => Task.FromResult(_unfinishedJobs));
        _jobsManager
            .Resume(_stoppedJob)
            .Returns(_ =>
            {
                _unfinishedJobs = ImmutableList.Create(new JobState { Id = _stoppedJob, Status = JobStatus.Running, Request = request });
                return Task.FromResult(true);
            });
    }

    async Task Because() => _result = await _observer.Replay();

    [Fact] void should_return_the_resumed_job() => _result.ShouldEqual(_stoppedJob);
    [Fact] void should_resume_the_stopped_job_once() => _jobsManager.Received(1).Resume(_stoppedJob);
    [Fact] void should_not_start_a_new_replay_job() => _jobsManager.DidNotReceive().Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
    [Fact] void should_be_replaying() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Replaying);
}
