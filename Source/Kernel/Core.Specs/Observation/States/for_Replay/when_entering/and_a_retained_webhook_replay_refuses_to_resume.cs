// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.States.for_Replay.when_entering;

public class and_a_retained_webhook_replay_refuses_to_resume : given.a_replay_state
{
    JobId _job;
    ObserverState _result;

    void Establish()
    {
        _job = JobId.New();
        _storedState = _storedState with { IsReplaying = true, WebhookReplayRetained = true, HandledEventCount = 12UL };
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult<IImmutableList<JobState>>(
            ImmutableList.Create(new JobState { Id = _job, Status = JobStatus.Stopped, Request = new ReplayObserverRequest(_observerKey, _observerDefinition.Type, []) })));
        _jobsManager.Resume(_job).Returns(false);
    }

    async Task Because() => _result = await _state.OnEnter(_storedState);

    [Fact] void should_try_the_retained_job() => _jobsManager.Received(1).Resume(_job);
    [Fact] void should_not_report_the_job_as_started() => _state.LastStartedJobId.ShouldEqual(JobId.NotSet);
    [Fact] void should_not_start_from_the_beginning() => _jobsManager.DidNotReceive().Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
    [Fact] void should_preserve_handled_counts() => _result.HandledEventCount.ShouldEqual((EventCount)12UL);
    [Fact] void should_keep_the_resume_intent() => _result.WebhookReplayRetained.ShouldBeTrue();
}
