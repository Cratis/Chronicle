// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_starting_or_resuming_an_observer_job;

public class and_a_stopped_job_resumes : given.a_jobs_manager
{
    JobState _stopped;

    void Establish()
    {
        _stopped = AJob(JobStatus.Stopped);
        HasJobs(_stopped);
        _jobsManager.Resume(_stopped.Id).Returns(true);
    }

    async Task Because() => await StartOrResume();

    [Fact] void should_report_the_resumed_job() => _result.ShouldEqual(_stopped.Id);
    [Fact] void should_tell_the_caller_it_is_resuming() => _onResumeWasCalled.ShouldBeTrue();
    [Fact] async Task should_ask_for_the_job_to_be_resumed() => await _jobsManager.Received(1).Resume(_stopped.Id);
    [Fact] async Task should_not_start_a_new_job() => await _jobsManager.DidNotReceive().Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
}
