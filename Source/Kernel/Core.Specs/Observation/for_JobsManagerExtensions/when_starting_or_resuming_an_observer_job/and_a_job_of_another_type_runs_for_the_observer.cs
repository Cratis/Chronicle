// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_starting_or_resuming_an_observer_job;

/// <summary>
/// The unfinished jobs span every job type. A replay running for the same observer is not a catch-up job, and taking
/// it for one would leave the catch-up the observer asked for never started.
/// </summary>
public class and_a_job_of_another_type_runs_for_the_observer : given.a_jobs_manager
{
    static readonly JobId _started = JobId.New();

    void Establish()
    {
        HasJobs(
            new JobState { Id = JobId.New(), Status = JobStatus.Running, Request = new ReplayObserverRequest(_observerKey, ObserverType.Reactor, []) },
            new JobState { Id = JobId.New(), Status = JobStatus.Stopped, Request = new ReplayObserverRequest(_observerKey, ObserverType.Reactor, []) });

        _jobsManager
            .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_started)));
    }

    async Task Because() => await StartOrResume();

    [Fact] void should_start_a_catch_up_job() => _result.ShouldEqual(_started);
    [Fact] void should_not_take_the_replay_for_a_running_catch_up() => _onAlreadyRunningJobWasCalled.ShouldBeFalse();
    [Fact] async Task should_not_resume_the_stopped_replay() => await _jobsManager.DidNotReceive().Resume(Arg.Any<JobId>());
}
