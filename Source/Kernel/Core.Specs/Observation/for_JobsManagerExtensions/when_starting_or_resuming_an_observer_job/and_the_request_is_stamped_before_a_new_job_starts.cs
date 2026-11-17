// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_starting_or_resuming_an_observer_job;

public class and_the_request_is_stamped_before_a_new_job_starts : given.a_jobs_manager
{
    static readonly JobId _started = JobId.New();
    CatchUpObserverRequest _stamped;
    CatchUpObserverRequest _stampedFrom;
    bool _startNewWasCalledBeforeStamping;

    void Establish()
    {
        _stamped = _request with { OwnershipEpoch = 7 };
        _jobsManager
            .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_started)));
    }

    async Task Because() => await StartOrResumeWith(stampBeforeStart: request =>
    {
        _stampedFrom = request;
        _startNewWasCalledBeforeStamping = _onStartNewWasCalled;
        return Task.FromResult(_stamped);
    });

    [Fact] void should_stamp_the_created_request() => _stampedFrom.ShouldEqual(_request);
    [Fact] void should_stamp_after_telling_the_caller() => _startNewWasCalledBeforeStamping.ShouldBeTrue();
    [Fact] async Task should_start_the_job_with_the_stamped_request() => await _jobsManager.Received(1).Start<ICatchUpObserver, CatchUpObserverRequest>(_stamped);
    [Fact] void should_report_the_started_job() => _result.ShouldEqual(_started);
}
