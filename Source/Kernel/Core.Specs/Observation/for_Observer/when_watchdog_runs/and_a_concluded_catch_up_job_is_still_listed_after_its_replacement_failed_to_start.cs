// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

/// <summary>
/// A catch-up job that has reported back stays listed as running until it is finalized, but it owns nothing. When the
/// replacement catch-up asked for after it failed to start, the observer is left preparing catch-up with no job to
/// finish it. Counting the concluded job as a running one kept the watchdog from rescuing the observer for as long as
/// its finalization was slow or failed (Cratis/Chronicle#4548).
/// </summary>
public class and_a_concluded_catch_up_job_is_still_listed_after_its_replacement_failed_to_start : given.an_observer_with_client_owned_subscription
{
    static readonly JobId _concludedJob = JobId.New();
    static readonly EventSequenceNumber _lastHandledByConcludedJob = 5UL;

    bool _wasPreparingCatchupAfterFailedStart;
    bool _isPreparingCatchupAfterWatchdog;

    void Establish()
    {
        _connectedClientsGrain.IsConnected(_connectedClient.ConnectionId).Returns(Task.FromResult(true));

        _jobsManager
            .GetJobs(Arg.Any<JobQuery>())
            .Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList.Create(new JobState
            {
                Id = _concludedJob,
                Status = JobStatus.Running,
                Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, [])
            })));

        // An event was appended at the boundary, so the conclusion asks for a replacement - which fails to start.
        _eventSequence.GetTailSequenceNumber().Returns(_lastHandledByConcludedJob.Next());
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(_lastHandledByConcludedJob.Next());

        _appendedEventsQueues.ClearReceivedCalls();
        _jobsManager.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.CaughtUp(_concludedJob, _lastHandledByConcludedJob);
        _wasPreparingCatchupAfterFailedStart = await _observer.IsPreparingCatchup();

        // The boundary event has since been handled live, so re-routing finds the observer caught up.
        _eventSequence.GetTailSequenceNumber().Returns(_lastHandledByConcludedJob);
        _appendedEventsQueues.ClearReceivedCalls();

        await _observer.RunWatchdogAsync();
        _isPreparingCatchupAfterWatchdog = await _observer.IsPreparingCatchup();
    }

    [Fact] void should_be_left_preparing_catch_up_when_the_replacement_failed_to_start() => _wasPreparingCatchupAfterFailedStart.ShouldBeTrue();

    [Fact] void should_have_asked_for_a_replacement_job() => _jobsManager
        .Received(1)
        .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());

    [Fact] void should_clear_the_stranded_catch_up_preparation() => _isPreparingCatchupAfterWatchdog.ShouldBeFalse();

    [Fact] void should_re_route_the_observer_back_onto_its_queue() => _appendedEventsQueues
        .Received(1)
        .Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());
}
