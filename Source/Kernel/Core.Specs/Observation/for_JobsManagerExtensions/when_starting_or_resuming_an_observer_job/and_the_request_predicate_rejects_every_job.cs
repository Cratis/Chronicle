// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_starting_or_resuming_an_observer_job;

public class and_the_request_predicate_rejects_every_job : given.a_jobs_manager
{
    static readonly JobId _started = JobId.New();

    void Establish()
    {
        HasJobs(AJob(JobStatus.Running), AJob(JobStatus.Stopped));

        _jobsManager
            .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_started)));
    }

    async Task Because() => await StartOrResume(requestPredicate: _ => false);

    [Fact] void should_start_a_new_job() => _result.ShouldEqual(_started);
    [Fact] void should_not_consider_the_rejected_running_job() => _onAlreadyRunningJobWasCalled.ShouldBeFalse();
    [Fact] async Task should_not_resume_the_rejected_stopped_job() => await _jobsManager.DidNotReceive().Resume(Arg.Any<JobId>());
}
