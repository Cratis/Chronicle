// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_starting_or_resuming_an_observer_job;

/// <summary>
/// A job stopped before it was prepared is refused when resumed, so it blocks its work for good. When asked to, the
/// stopped job is dropped and a new one started in its place.
/// </summary>
public class and_a_stopped_job_was_never_prepared : given.a_jobs_manager
{
    JobState _stopped;
    JobId _started;

    void Establish()
    {
        _stopped = AJob(JobStatus.Stopped);
        _started = JobId.New();
        HasJobs(_stopped);
        _jobsManager
            .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_started)));
    }

    async Task Because() => await StartOrResume(dropJobsThatCanNeverResume: true);

    [Fact] async Task should_drop_it() => await _jobsManager.Received(1).Delete(_stopped.Id);
    [Fact] async Task should_not_try_to_resume_it() => await _jobsManager.DidNotReceive().Resume(Arg.Any<JobId>());
    [Fact] async Task should_start_a_new_job() => await _jobsManager.Received(1).Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
    [Fact] void should_report_the_started_job() => _result.ShouldEqual(_started);
}
