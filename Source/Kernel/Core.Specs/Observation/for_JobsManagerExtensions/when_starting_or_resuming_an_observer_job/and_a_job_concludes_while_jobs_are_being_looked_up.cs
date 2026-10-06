// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_starting_or_resuming_an_observer_job;

/// <summary>
/// A job concluding while the lookup is in flight may not be in a listing taken before it was remembered, so that
/// listing says nothing about whether it has finished. It is kept for the next lookup to judge, and is still excluded
/// as an owner (Cratis/Chronicle#4548).
/// </summary>
public class and_a_job_concludes_while_jobs_are_being_looked_up : given.a_jobs_manager
{
    readonly JobId _concludingJob = JobId.New();
    readonly JobId _startedJob = JobId.New();
    HashSet<JobId> _concludedJobs;

    void Establish()
    {
        _concludedJobs = [];
        var otherObserversJob = AJob(JobStatus.Running, _observerKey with { ObserverId = "another-observer" });
        _jobsManager
            .GetJobs(Arg.Any<JobQuery>())
            .Returns(_ =>
            {
                _concludedJobs.Add(_concludingJob);
                return Task.FromResult<IImmutableList<JobState>>(ImmutableList.Create(otherObserversJob));
            });
        _jobsManager
            .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_startedJob)));
    }

    async Task Because() => await StartOrResume(concludedJobs: _concludedJobs);

    [Fact] void should_keep_remembering_the_job_that_concluded_during_the_lookup() => _concludedJobs.ShouldContain(_concludingJob);
    [Fact] void should_report_the_started_job() => _result.ShouldEqual(_startedJob);
}
