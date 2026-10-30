// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_getting_outstanding_jobs;

public class and_a_job_is_being_removed : given.a_jobs_manager
{
    JobState _removing;
    JobQuery _query;
    IImmutableList<JobState> _jobs;

    void Establish()
    {
        _removing = AJob(JobStatus.Removing);
        _jobsManager.GetJobs(Arg.Do<JobQuery>(query => _query = query))
            .Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList.Create(_removing)));
    }

    async Task Because() => _jobs = await _jobsManager.GetOutstandingJobs();

    [Fact] void should_not_limit_the_lookup() => _query.Take.ShouldEqual(0);
    [Fact] void should_include_draining_jobs() => _query.Statuses.ShouldContainOnly(JobStatus.None, JobStatus.PreparingJob, JobStatus.PreparingSteps, JobStatus.StartingSteps, JobStatus.Running, JobStatus.Stopped, JobStatus.Removing);
    [Fact] void should_return_the_removing_job() => _jobs.ShouldContain(_removing);
}
