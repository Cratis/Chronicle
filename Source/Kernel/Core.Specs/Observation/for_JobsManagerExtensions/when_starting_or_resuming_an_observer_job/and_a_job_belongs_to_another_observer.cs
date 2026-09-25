// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_starting_or_resuming_an_observer_job;

/// <summary>
/// The jobs manager answers with every job of the type, across every observer. Only the ones for this observer key
/// may be considered - otherwise one observer rides on another observer's running job and never does its own work.
/// </summary>
public class and_a_job_belongs_to_another_observer : given.a_jobs_manager
{
    static readonly JobId _started = JobId.New();

    void Establish()
    {
        var otherObserver = new ObserverKey("some-other-observer", "some-event-store", "some-namespace", EventSequenceId.Log);
        HasJobs(AJob(JobStatus.Running, otherObserver), AJob(JobStatus.Stopped, otherObserver));

        _jobsManager
            .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_started)));
    }

    async Task Because() => await StartOrResume();

    [Fact] void should_start_a_job_of_its_own() => _result.ShouldEqual(_started);
    [Fact] void should_not_consider_the_other_observers_running_job() => _onAlreadyRunningJobWasCalled.ShouldBeFalse();
    [Fact] async Task should_not_resume_the_other_observers_stopped_job() => await _jobsManager.DidNotReceive().Resume(Arg.Any<JobId>());
}
