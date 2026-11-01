// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_starting_or_resuming_an_observer_job;

/// <summary>
/// Removing a refused job that holds no steps is a drop like any other: a caller that owns a per-turn budget of drops
/// and has used it up gets nothing removed in this turn, so a turn never deletes more jobs than its budget allows.
/// </summary>
public class and_a_stepless_job_refuses_to_resume_with_the_drop_budget_used_up : given.a_jobs_manager
{
    JobState _stopped;
    int _reportedDrops;

    void Establish()
    {
        _stopped = APreparedJob(JobStatus.Stopped);
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
        onJobsDropped: dropped => _reportedDrops += dropped);

    [Fact] async Task should_not_remove_the_job() => await _jobsManager.DidNotReceive().Delete(_stopped.Id);
    [Fact] void should_not_report_a_drop() => _reportedDrops.ShouldEqual(0);
    [Fact] void should_not_report_a_job() => _result.ShouldEqual(JobId.NotSet);
}
