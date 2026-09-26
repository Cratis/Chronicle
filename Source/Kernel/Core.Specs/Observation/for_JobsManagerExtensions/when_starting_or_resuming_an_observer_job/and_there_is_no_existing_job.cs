// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_starting_or_resuming_an_observer_job;

public class and_there_is_no_existing_job : given.a_jobs_manager
{
    static readonly JobId _started = JobId.New();

    void Establish() =>
        _jobsManager
            .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_started)));

    async Task Because() => await StartOrResume();

    [Fact] void should_report_the_started_job() => _result.ShouldEqual(_started);
    [Fact] void should_tell_the_caller_it_is_starting_a_new_job() => _onStartNewWasCalled.ShouldBeTrue();
    [Fact] async Task should_start_the_job_with_the_request() => await _jobsManager.Received(1).Start<ICatchUpObserver, CatchUpObserverRequest>(_request);
}
