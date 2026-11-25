// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_starting_or_resuming_an_observer_job;

public class and_a_stopped_job_holds_an_obsolete_claim : given.a_jobs_manager
{
    static readonly JobId _started = JobId.New();
    JobState _stopped;

    void Establish()
    {
        _stopped = AJob(JobStatus.Stopped);
        HasJobs(_stopped);
        _jobsManager
            .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_started)));
    }

    async Task Because() => await StartOrResumeWith(isObsolete: _ => true);

    [Fact] async Task should_not_resume_the_job() => await _jobsManager.DidNotReceive().Resume(Arg.Any<JobId>());
    [Fact] void should_not_tell_the_caller_it_is_resuming() => _onResumeWasCalled.ShouldBeFalse();
    [Fact] void should_start_a_new_job() => _result.ShouldEqual(_started);
}
