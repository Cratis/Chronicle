// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_starting_or_resuming_an_observer_job;

/// <summary>
/// Dropping is bounded. The allowed number is dropped and the rest still blocks the work, so nothing is resumed or
/// started; the caller re-arms its own retry.
/// </summary>
public class and_there_are_more_jobs_that_can_never_resume_than_may_be_dropped : given.a_jobs_manager
{
    const int MaxJobsToDrop = 2;

    JobState[] _stopped;
    int _reportedDrops;

    void Establish()
    {
        _stopped = [AJob(JobStatus.Stopped), AJob(JobStatus.Stopped), AJob(JobStatus.Stopped), AJob(JobStatus.Stopped)];
        HasJobs(_stopped);
    }

    async Task Because() => _result = await _jobsManager.StartOrResumeObserverJobReportingDrops<ICatchUpObserver, CatchUpObserverRequest>(
        NullLogger.Instance,
        () => _request,
        null,
        null,
        null,
        null,
        null,
        null,
        MaxJobsToDrop,
        dropped => _reportedDrops += dropped);

    [Fact] async Task should_only_drop_the_allowed_number() => await _jobsManager.Received(MaxJobsToDrop).Delete(Arg.Any<JobId>());
    [Fact] void should_report_the_dropped_jobs() => _reportedDrops.ShouldEqual(MaxJobsToDrop);
    [Fact] void should_not_report_a_job() => _result.ShouldEqual(JobId.NotSet);
    [Fact] async Task should_not_resume_any_job() => await _jobsManager.DidNotReceive().Resume(Arg.Any<JobId>());
    [Fact] async Task should_not_start_a_new_job() => await _jobsManager.DidNotReceive().Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
}
