// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_starting_or_resuming_an_observer_job;

/// <summary>
/// Only the stopped job that can never resume is dropped. A prepared stopped job is still resumed.
/// </summary>
public class and_a_never_prepared_stopped_job_sits_beside_a_prepared_one : given.a_jobs_manager
{
    JobState _neverPrepared;
    JobState _prepared;

    void Establish()
    {
        _neverPrepared = AJob(JobStatus.Stopped);
        _prepared = APreparedJob(JobStatus.Stopped);
        HasJobs(_neverPrepared, _prepared);
        _jobsManager.Resume(_prepared.Id).Returns(true);
    }

    async Task Because() => await StartOrResume(dropJobsThatCanNeverResume: true);

    [Fact] async Task should_drop_the_job_that_was_never_prepared() => await _jobsManager.Received(1).Delete(_neverPrepared.Id);
    [Fact] async Task should_not_drop_the_prepared_job() => await _jobsManager.DidNotReceive().Delete(_prepared.Id);
    [Fact] async Task should_resume_the_prepared_job() => await _jobsManager.Received(1).Resume(_prepared.Id);
    [Fact] async Task should_not_start_a_new_job() => await _jobsManager.DidNotReceive().Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
    [Fact] void should_report_the_resumed_job() => _result.ShouldEqual(_prepared.Id);
}
