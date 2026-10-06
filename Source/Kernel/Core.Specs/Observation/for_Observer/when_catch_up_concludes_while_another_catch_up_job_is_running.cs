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
/// Only the catch-up job that reported back has concluded. Another catch-up job listed alongside it while it finalizes
/// is live and still owns its work, so routing must adopt it rather than start a third job over the same events and
/// deliver them twice (Cratis/Chronicle#4548).
/// </summary>
public class when_catch_up_concludes_while_another_catch_up_job_is_running : given.an_observer_with_subscription
{
    static readonly JobId _finishingJob = JobId.New();
    static readonly JobId _liveJob = JobId.New();

    void Establish()
    {
        _jobsManager
            .GetJobs(Arg.Any<JobQuery>())
            .Returns(Task.FromResult<IImmutableList<JobState>>(
                ImmutableList.Create(
                    new JobState
                    {
                        Id = _finishingJob,
                        Status = JobStatus.Running,
                        Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, [])
                    },
                    new JobState
                    {
                        Id = _liveJob,
                        Status = JobStatus.Running,
                        Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, (EventSequenceNumber)2UL, [])
                    })));

        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)2UL);
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns((EventSequenceNumber)2UL);
    }

    Task Because() => _observer.CaughtUp(_finishingJob, 1UL);

    [Fact]
    async Task should_not_start_another_catch_up_job() =>
        await _jobsManager.DidNotReceive().Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());

    [Fact] async Task should_still_be_preparing_catch_up_for_the_live_job() => (await _observer.IsPreparingCatchup()).ShouldBeTrue();
}
