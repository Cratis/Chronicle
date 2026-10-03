// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_getting_unfinished_jobs;

/// <summary>
/// Finished jobs are retained and outnumber the live ones by thousands. Asking for every job on each observer subscribe
/// and unsubscribe deserialized all of them each time, so the filtering has to happen in the query.
/// </summary>
public class and_finished_jobs_are_retained : Specification
{
    IJobsManager _jobsManager;
    JobQuery _query;
    IImmutableList<JobState> _unfinished;
    IImmutableList<JobState> _result;

    void Establish()
    {
        _jobsManager = Substitute.For<IJobsManager>();
        _unfinished = ImmutableList.Create(new JobState { Id = JobId.New(), Status = JobStatus.Running });
        _jobsManager.GetJobs(Arg.Do<JobQuery>(query => _query = query)).Returns(Task.FromResult(_unfinished));
    }

    async Task Because() => _result = await _jobsManager.GetUnfinishedJobs();

    [Fact] void should_not_load_every_job() => _jobsManager.DidNotReceive().GetAllJobs();
    [Fact] void should_ask_for_the_unfinished_statuses() => _query.Statuses.ShouldContainOnly(JobStatus.None, JobStatus.PreparingJob, JobStatus.PreparingSteps, JobStatus.StartingSteps, JobStatus.Running, JobStatus.Stopped);
    [Fact] void should_not_limit_how_many_it_gets() => _query.Take.ShouldEqual(0);
    [Fact] void should_not_narrow_to_a_job_type() => _query.Type.ShouldBeNull();
    [Fact] void should_return_what_the_jobs_manager_found() => _result.ShouldEqual(_unfinished);
}
