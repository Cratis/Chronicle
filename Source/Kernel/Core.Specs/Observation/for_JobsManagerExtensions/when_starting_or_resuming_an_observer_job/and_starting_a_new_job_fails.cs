// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_starting_or_resuming_an_observer_job;

/// <summary>
/// Starting can legitimately fail - a node joining a cluster where another node already took the same observer job.
/// The observer's watchdog retries later, so a failed start must not take the caller down, and must not be reported
/// as a job id nobody can wait on.
/// </summary>
public class and_starting_a_new_job_fails : given.a_jobs_manager
{
    void Establish() =>
        _jobsManager
            .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Failed(StartJobError.Unknown)));

    async Task Because() => await StartOrResume();

    [Fact] void should_not_report_a_job() => _result.ShouldEqual(JobId.NotSet);
    [Fact] void should_still_have_told_the_caller_it_was_starting() => _onStartNewWasCalled.ShouldBeTrue();
}
