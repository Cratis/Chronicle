// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_starting_or_resuming_an_observer_job;

/// <summary>
/// A job that cannot be dropped still blocks its work, so nothing is started. The failure must not reach the caller,
/// who re-arms its own retry from the missing job id.
/// </summary>
public class and_dropping_a_job_that_can_never_resume_fails : given.a_jobs_manager
{
    Exception? _error;

    void Establish()
    {
        var stopped = AJob(JobStatus.Stopped);
        HasJobs(stopped);
        _jobsManager.Configure().Delete(stopped.Id).Returns(Task.FromException(new InvalidOperationException("Delete failed")));
    }

    async Task Because() => _error = await Catch.Exception(() => StartOrResume(maxJobsToDrop: 10));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_not_report_a_job() => _result.ShouldEqual(JobId.NotSet);
    [Fact] async Task should_not_start_a_new_job() => await _jobsManager.DidNotReceive().Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
}
