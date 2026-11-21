// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_catch_up_makes_progress_after_the_stale_retry_timer_fires : given.an_observer_with_subscription
{
    static readonly JobId _retryJob = JobId.New();

    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        await _observer.CaughtUp(JobId.New(), 42L);
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns((EventSequenceNumber)50UL);
        await _observer.CaughtUp(JobId.New(), 42L);
        await _observer.CaughtUp(JobId.New(), 42L);
        _observer.SetCatchupRecoveryAttemptsForTesting(3);
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)50UL);
        _jobsManager
            .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_retryJob)));
    }

    async Task Because()
    {
        await _silo.TimerRegistry.FireAllAsync();
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(EventSequenceNumber.Unavailable);
        await _observer.CaughtUp(_retryJob, 50L, 50L);
    }

    [Fact] async Task should_start_the_retry_job() => await _jobsManager.Received(1).Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
    [Fact] async Task should_advance_past_the_scanned_events() => (await _observer.GetState()).NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)51UL);
    [Fact] async Task should_remain_subscribed() => (await _observer.IsSubscribed()).ShouldBeTrue();
    [Fact] void should_reset_the_recovery_attempts() => _observer.CatchupRecoveryAttempts.ShouldEqual(0);
    [Fact] async Task should_not_back_off() => (await _observer.IsCatchUpBackedOff()).ShouldBeFalse();
}
