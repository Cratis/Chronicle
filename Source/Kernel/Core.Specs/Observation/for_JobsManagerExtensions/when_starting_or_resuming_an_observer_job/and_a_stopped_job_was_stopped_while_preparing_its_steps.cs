// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Observation.Jobs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_starting_or_resuming_an_observer_job;

/// <summary>
/// A catch-up job that was stopped (a restart) while it was still preparing thousands of partition steps has already counted
/// them all in its progress, but never started a step. Cratis.Choreography refuses to resume it for good, so it has to be
/// removed - otherwise the observer stays behind the event sequence and every later catch-up finds the same stopped job.
/// </summary>
public class and_a_stopped_job_was_stopped_while_preparing_its_steps : given.a_jobs_manager
{
    JobState _stopped;

    void Establish()
    {
        _stopped = AJob(JobStatus.Stopped);
        _stopped.Progress = new() { TotalSteps = 10000 };
        _stopped.StatusChanges =
        [
            new JobStatusChanged { Status = JobStatus.PreparingJob, Occurred = DateTimeOffset.UtcNow },
            new JobStatusChanged { Status = JobStatus.PreparingSteps, Occurred = DateTimeOffset.UtcNow },
            new JobStatusChanged { Status = JobStatus.Stopped, Occurred = DateTimeOffset.UtcNow }
        ];
        HasJobs(_stopped);
        _jobsManager.Resume(_stopped.Id).Returns(false);
    }

    async Task Because() => _result = await _jobsManager.StartOrResumeObserverJobReportingDrops<ICatchUpObserver, CatchUpObserverRequest>(
        NullLogger.Instance,
        () => _request,
        requestPredicate: null,
        onAlreadyRunningJob: null,
        onResume: null,
        onStartNew: null,
        onResumeRefused: null,
        concludedJobs: null,
        maxJobsToDrop: 0,
        onJobsDropped: null);

    [Fact] async Task should_remove_the_job() => await _jobsManager.Received(1).Delete(_stopped.Id);
    [Fact] void should_not_report_a_job() => _result.ShouldEqual(JobId.NotSet);
}
