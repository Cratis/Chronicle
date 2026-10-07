// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

/// <summary>
/// The watchdog interleaves with a catch-up job reporting back. While the concluded job is handed over, it no longer
/// counts as an owner and its successor is still being started, so nothing listed owns catch-up. Rescuing in that
/// window cleared a preparation the handover still needed and counted a recovery towards quarantine for a strand that
/// never happened (Cratis/Chronicle#4548).
/// </summary>
public class and_a_concluded_catch_up_job_is_being_handed_over : given.an_observer_with_client_owned_subscription
{
    static readonly JobId _concludedJob = JobId.New();
    static readonly JobId _successorJob = JobId.New();
    static readonly EventSequenceNumber _lastHandledByConcludedJob = 5UL;

    readonly TaskCompletionSource<Result<JobId, StartJobError>> _heldStart = new(TaskCreationOptions.RunContinuationsAsynchronously);

    bool _isPreparingCatchupAfterWatchdog;
    int _recoveryAttemptsAfterWatchdog;

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

        _jobsManager
            .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(_ => _heldStart.Task);

        // An event was appended at the boundary, so the conclusion routes into a catch-up for its successor.
        _eventSequence.GetTailSequenceNumber().Returns(_lastHandledByConcludedJob.Next());
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(_lastHandledByConcludedJob.Next());

        _jobsManager.ClearReceivedCalls();
    }

    async Task Because()
    {
        var caughtUp = _observer.CaughtUp(_concludedJob, _lastHandledByConcludedJob);

        var watchdog = _observer.RunWatchdogAsync();
        _isPreparingCatchupAfterWatchdog = await _observer.IsPreparingCatchup();
        _recoveryAttemptsAfterWatchdog = _observer.CatchupRecoveryAttempts;

        _heldStart.SetResult(Result<JobId, StartJobError>.Success(_successorJob));
        await Task.WhenAll(caughtUp, watchdog);
    }

    [Fact] void should_still_be_preparing_catch_up() => _isPreparingCatchupAfterWatchdog.ShouldBeTrue();
    [Fact] void should_not_count_a_recovery_towards_quarantine() => _recoveryAttemptsAfterWatchdog.ShouldEqual(0);

    [Fact] void should_start_only_the_successor_job() => _jobsManager
        .Received(1)
        .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
}
