// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_starting_or_resuming_an_observer_job;

/// <summary>
/// With drops left in the caller's budget, a refused job that holds no steps is removed and the removal is reported,
/// so the caller takes it from its budget.
/// </summary>
public class and_a_stepless_job_refuses_to_resume_within_the_drop_budget : given.a_jobs_manager
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
        maxJobsToDrop: 1,
        onJobsDropped: dropped => _reportedDrops += dropped);

    [Fact] async Task should_remove_the_job() => await _jobsManager.Received(1).Delete(_stopped.Id);
    [Fact] void should_report_one_drop() => _reportedDrops.ShouldEqual(1);
    [Fact] void should_not_report_a_job() => _result.ShouldEqual(JobId.NotSet);
}
