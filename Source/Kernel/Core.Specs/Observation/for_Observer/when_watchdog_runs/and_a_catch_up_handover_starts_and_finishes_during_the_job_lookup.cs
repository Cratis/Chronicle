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
/// CaughtUp can begin and complete - successor started and no acquisition left in flight - entirely while the
/// watchdog's job lookup is awaited. The lookup answers with a listing taken before the successor started: it holds
/// only the concluded job, which owns nothing. Read as it stands, that listing says nobody owns catch-up, although the
/// successor that does was started meanwhile (#4548).
/// </summary>
public class and_a_catch_up_handover_starts_and_finishes_during_the_job_lookup : given.an_observer_with_client_owned_subscription
{
    static readonly JobId _concludedJob = JobId.New();
    static readonly JobId _successorJob = JobId.New();
    static readonly EventSequenceNumber _lastHandledByConcludedJob = 5UL;

    readonly TaskCompletionSource<IImmutableList<JobState>> _heldLookup = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _lookupEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);

    IImmutableList<JobState> _jobsBeforeTheSuccessorStarted;
    bool _isPreparingCatchupAfterWatchdog;
    int _recoveryAttemptsAfterWatchdog;

    async Task Establish()
    {
        _connectedClientsGrain.IsConnected(_connectedClient.ConnectionId).Returns(true);
        _jobsBeforeTheSuccessorStarted = ImmutableList.Create(new JobState
        {
            Id = _concludedJob,
            Status = JobStatus.Running,
            Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, [])
        });
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult(_jobsBeforeTheSuccessorStarted));

        // Adoption raises preparation without leaving an acquisition in flight before the watchdog starts.
        await _observer.CatchUp();

        var lookupCount = 0;
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(_ =>
        {
            if (++lookupCount != 1) return Task.FromResult(_jobsBeforeTheSuccessorStarted);
            _lookupEntered.TrySetResult();
            return _heldLookup.Task;
        });
        _jobsManager.Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_successorJob)));
        _eventSequence.GetTailSequenceNumber().Returns(_lastHandledByConcludedJob.Next());
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(_lastHandledByConcludedJob.Next());
    }

    async Task Because()
    {
        var watchdog = _observer.RunWatchdogAsync();
        try
        {
            await _lookupEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

            // The whole handover runs to completion while only the watchdog's lookup is held.
            await _observer.CaughtUp(_concludedJob, _lastHandledByConcludedJob).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
        finally
        {
            _heldLookup.TrySetResult(_jobsBeforeTheSuccessorStarted);
        }

        await watchdog.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _isPreparingCatchupAfterWatchdog = await _observer.IsPreparingCatchup();
        _recoveryAttemptsAfterWatchdog = _observer.CatchupRecoveryAttempts;
    }

    [Fact] void should_start_only_the_successor_and_not_route_into_another() => _jobsManager.Received(1).Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
    [Fact] void should_preserve_catch_up_preparation_for_the_successor() => _isPreparingCatchupAfterWatchdog.ShouldBeTrue();
    [Fact] void should_not_count_a_recovery_towards_quarantine() => _recoveryAttemptsAfterWatchdog.ShouldEqual(0);
}
