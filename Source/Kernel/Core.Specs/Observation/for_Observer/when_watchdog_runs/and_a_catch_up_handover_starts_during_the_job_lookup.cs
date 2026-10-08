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
/// CaughtUp can start after the watchdog has checked for a handover but before its job lookup returns.
/// The concluded job no longer owns catch-up, but the held successor acquisition does (#4548).
/// </summary>
public class and_a_catch_up_handover_starts_during_the_job_lookup : given.an_observer_with_client_owned_subscription
{
    static readonly JobId _concludedJob = JobId.New();
    static readonly JobId _successorJob = JobId.New();
    static readonly EventSequenceNumber _lastHandledByConcludedJob = 5UL;

    readonly TaskCompletionSource<IImmutableList<JobState>> _heldLookup = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _lookupEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<Result<JobId, StartJobError>> _heldStart = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _startEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);

    IImmutableList<JobState> _jobs;
    bool _isPreparingCatchupAfterWatchdog;
    int _recoveryAttemptsAfterWatchdog;
    int _routingLookupsAfterWatchdog;

    async Task Establish()
    {
        _connectedClientsGrain.IsConnected(_connectedClient.ConnectionId).Returns(true);
        _jobs = ImmutableList.Create(new JobState
        {
            Id = _concludedJob,
            Status = JobStatus.Running,
            Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, [])
        });
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult(_jobs));

        // Adoption raises preparation without leaving an acquisition in flight before the watchdog starts.
        await _observer.CatchUp();

        var lookupCount = 0;
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(_ =>
        {
            if (++lookupCount != 1) return Task.FromResult(_jobs);
            _lookupEntered.TrySetResult();
            return _heldLookup.Task;
        });
        _jobsManager.Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>()).Returns(_ =>
        {
            _startEntered.TrySetResult();
            return _heldStart.Task;
        });
        _eventSequence.GetTailSequenceNumber().Returns(_lastHandledByConcludedJob.Next());
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(_lastHandledByConcludedJob.Next());
        _eventSequence.ClearReceivedCalls();
    }

    async Task Because()
    {
        var watchdog = _observer.RunWatchdogAsync();
        await _lookupEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        var caughtUp = _observer.CaughtUp(_concludedJob, _lastHandledByConcludedJob);
        await _startEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        try
        {
            _heldLookup.SetResult(_jobs);
            await watchdog.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            _isPreparingCatchupAfterWatchdog = await _observer.IsPreparingCatchup();
            _recoveryAttemptsAfterWatchdog = _observer.CatchupRecoveryAttempts;
            _routingLookupsAfterWatchdog = _eventSequence.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(_eventSequence.GetTailSequenceNumber));
        }
        finally
        {
            _heldStart.SetResult(Result<JobId, StartJobError>.Success(_successorJob));
            await Task.WhenAll(caughtUp, watchdog).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
    }

    [Fact] void should_preserve_catch_up_preparation() => _isPreparingCatchupAfterWatchdog.ShouldBeTrue();
    [Fact] void should_not_count_a_recovery_towards_quarantine() => _recoveryAttemptsAfterWatchdog.ShouldEqual(0);
    [Fact] void should_not_route_again() => _routingLookupsAfterWatchdog.ShouldEqual(1);
}
