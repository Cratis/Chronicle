// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_starting_or_resuming_an_observer_job;

public class and_a_job_stayed_in_preparation_without_a_step : given.a_jobs_manager
{
    static readonly JobId _started = JobId.New();
    JobState _abandoned;

    void Establish()
    {
        _abandoned = AJob(JobStatus.StartingSteps);
        _abandoned.Created = DateTimeOffset.UtcNow - TimeSpan.FromHours(2);
        _abandoned.StatusChanges = [new JobStatusChanged { Status = JobStatus.StartingSteps, Occurred = DateTimeOffset.UtcNow - TimeSpan.FromHours(2) }];
        HasJobs(_abandoned);
        _jobsManager
            .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_started)));
    }

    async Task Because() => await StartOrResume();

    [Fact] async Task should_delete_the_abandoned_job() => await _jobsManager.Received(1).Delete(_abandoned.Id);
    [Fact] void should_not_treat_it_as_running() => _onAlreadyRunningJobWasCalled.ShouldBeFalse();
    [Fact] void should_start_a_new_job() => _result.ShouldEqual(_started);
}
