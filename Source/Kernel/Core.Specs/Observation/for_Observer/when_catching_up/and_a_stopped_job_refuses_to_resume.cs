// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catching_up;

/// <summary>
/// Catch-up finds a stopped job and asks for it to be resumed, and resuming is allowed to refuse - the job was never
/// prepared, or its observer is no longer subscribed. A refused job stays exactly as it was: it will not run, will not
/// finalize, and will never report back, so nothing is ever going to lower the preparing flag.
/// Leaving it raised is what wedged production. Handle drops every live event while it is up and Observing skips its
/// missed-events check, so the observer never advances; the watchdog clears the flag and re-routes, catch-up finds the
/// same stopped job and it refuses again, and five rounds later the observer is quarantined for a strand that was only
/// ever a job nobody took. Thirty stopped catch-up jobs sat in the store this happened to.
/// </summary>
public class and_a_stopped_job_refuses_to_resume : given.an_observer_with_subscription
{
    static readonly JobId _stoppedJob = JobId.New();

    bool _isPreparingCatchupAfter;

    void Establish()
    {
        _jobsManager
            .GetJobsOfType<ICatchUpObserver, CatchUpObserverRequest>()
            .Returns(Task.FromResult<IImmutableList<JobState>>(
                ImmutableList.Create(new JobState
                {
                    Id = _stoppedJob,
                    Status = JobStatus.Stopped,
                    Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, [])
                })));

        _jobsManager.Resume(_stoppedJob).Returns(false);
    }

    async Task Because()
    {
        await _observer.CatchUp();
        _isPreparingCatchupAfter = await _observer.IsPreparingCatchup();
    }

    [Fact]
    async Task should_ask_for_the_stopped_job_to_be_resumed() =>
        await _jobsManager.Received(1).Resume(_stoppedJob);

    [Fact]
    void should_not_be_left_preparing_catch_up() =>
        _isPreparingCatchupAfter.ShouldBeFalse();
}
