// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Orleans.Core;
using Orleans.TestKit.Storage;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

/// <summary>
/// CaughtUp is still persisting its checkpoint, before routing or any successor acquisition exists.
/// Only the handover owns catch-up in this window (#4548).
/// </summary>
public class and_a_catch_up_handover_has_not_started_acquiring_its_successor : given.an_observer_with_client_owned_subscription
{
    static readonly JobId _concludedJob = JobId.New();
    static readonly EventSequenceNumber _lastHandledByConcludedJob = 5UL;

    readonly IStorage<ObserverState> _heldStorage = Substitute.For<IStorage<ObserverState>, IStorageStats>();
    readonly TaskCompletionSource _heldWrite = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _writeEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _recoveryEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);

    bool _isPreparingCatchupAfterWatchdog;
    int _recoveryAttemptsAfterWatchdog;

    public and_a_catch_up_handover_has_not_started_acquiring_its_successor()
    {
        _heldStorage.State = ObserverState.Empty;
        ((IStorageStats)_heldStorage).Stats.Returns(new TestStorageStats());
        _silo.Options.StorageFactory = type => type == typeof(ObserverState)
            ? _heldStorage
            : null!; // Let TestKit use its default storage for definitions and failures.
    }

    async Task Establish()
    {
        _connectedClientsGrain.IsConnected(_connectedClient.ConnectionId).Returns(true);
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList.Create(new JobState
        {
            Id = _concludedJob,
            Status = JobStatus.Running,
            Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, [])
        })));

        // Adoption raises preparation, with no acquisition left in flight when the handover starts.
        await _observer.CatchUp();
        var writeCount = 0;
        _heldStorage.WriteStateAsync().Returns(_ =>
        {
            if (++writeCount != 1) return Task.CompletedTask;
            _writeEntered.TrySetResult();
            return _heldWrite.Task;
        });
        _configurationProvider.GetFor(Arg.Any<string>()).Returns(_ =>
        {
            if (_observer.CatchupRecoveryAttempts > 0) _recoveryEntered.TrySetResult();
            return _observersConfig;
        });
        _appendedEventsQueues.ClearReceivedCalls();
        _jobsManager.ClearReceivedCalls();
    }

    async Task Because()
    {
        var caughtUp = _observer.CaughtUp(_concludedJob, _lastHandledByConcludedJob);
        await _writeEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        var watchdog = _observer.RunWatchdogAsync();
        try
        {
            // A rescue may wait for the held checkpoint write. Observe its decision before releasing that write.
            await Task.WhenAny(watchdog, _recoveryEntered.Task).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            _isPreparingCatchupAfterWatchdog = await _observer.IsPreparingCatchup();
            _recoveryAttemptsAfterWatchdog = _observer.CatchupRecoveryAttempts;
        }
        finally
        {
            _heldWrite.SetResult();
            await Task.WhenAll(caughtUp, watchdog).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
    }

    [Fact] void should_preserve_catch_up_preparation() => _isPreparingCatchupAfterWatchdog.ShouldBeTrue();
    [Fact] void should_not_count_a_recovery_towards_quarantine() => _recoveryAttemptsAfterWatchdog.ShouldEqual(0);
    [Fact] void should_route_only_for_the_handover() => _appendedEventsQueues.Received(1).Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());
    [Fact] void should_not_acquire_a_successor_job() => _jobsManager.DidNotReceive().Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
}
