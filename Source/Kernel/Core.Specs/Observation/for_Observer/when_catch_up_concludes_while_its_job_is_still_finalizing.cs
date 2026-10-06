// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// CatchUpObserver.OnAllStepsCompleted fires CaughtUp at the observer before the job itself is finalized, so the job
/// is still listed as running when CaughtUp routes the observer. When an event was appended at that boundary routing
/// finds the observer behind and asks for catch-up again - and finds the very job that has just reported back. That
/// job will never report back a second time, so nothing drives the new event and the observer is left preparing
/// catch-up, dropping live events and skipping its missed-events check, until the watchdog notices
/// (Cratis/Chronicle#4548).
/// </summary>
public class when_catch_up_concludes_while_its_job_is_still_finalizing : given.an_observer_with_subscription
{
    static readonly JobId _finishingJob = JobId.New();

    void Establish()
    {
        _jobsManager
            .GetJobs(Arg.Any<JobQuery>())
            .Returns(Task.FromResult<IImmutableList<JobState>>(
                ImmutableList.Create(new JobState
                {
                    Id = _finishingJob,
                    Status = JobStatus.Running,
                    Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, [])
                })));

        // The job handled 0 and 1; event 2 was appended for a new partition just before it reported back.
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)2UL);
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns((EventSequenceNumber)2UL);
    }

    Task Because() => _observer.CaughtUp(_finishingJob, 1UL);

    [Fact]
    async Task should_start_a_catch_up_job_for_the_event_appended_at_the_boundary() =>
        await _jobsManager.Received(1).Start<ICatchUpObserver, CatchUpObserverRequest>(
            Arg.Is<CatchUpObserverRequest>(request => request.FromEventSequenceNumber == (EventSequenceNumber)2UL));
}
