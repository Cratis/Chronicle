// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_starting_or_resuming_an_observer_job;

/// <summary>
/// The jobs manager reports a failed lookup as an empty listing. Taking that as proof that every remembered concluded
/// job has finished forgot them all, and a concluded job that was in fact still running became adoptable again on the
/// next lookup that listed it - handing catch-up to a job that will never report back (Cratis/Chronicle#4548).
/// </summary>
public class and_the_lookup_fails_while_a_concluded_job_is_still_running : given.a_jobs_manager
{
    readonly JobId _startedJob = JobId.New();
    HashSet<JobId> _concludedJobs;
    JobState _concludedJob;

    void Establish()
    {
        _concludedJob = AJob(JobStatus.Running);
        _concludedJobs = [_concludedJob.Id];
        _jobsManager
            .GetJobs(Arg.Any<JobQuery>())
            .Returns(
                Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty),
                Task.FromResult<IImmutableList<JobState>>(ImmutableList.Create(_concludedJob)));
        _jobsManager
            .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_startedJob)));
    }

    async Task Because()
    {
        await StartOrResume(concludedJobs: _concludedJobs);
        await StartOrResume(concludedJobs: _concludedJobs);
    }

    [Fact] void should_not_adopt_the_concluded_job_once_it_is_listed_again() => _result.ShouldEqual(_startedJob);
    [Fact] void should_still_remember_the_concluded_job() => _concludedJobs.ShouldContain(_concludedJob.Id);
}
