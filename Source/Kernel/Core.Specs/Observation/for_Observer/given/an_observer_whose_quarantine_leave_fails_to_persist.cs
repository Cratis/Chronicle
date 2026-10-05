// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_whose_quarantine_leave_fails_to_persist : an_observer_with_subscription
{
    protected readonly faultable_observer_state_storage _faultableStateStorage = new();
    protected readonly Exception _writeFailure = new InvalidOperationException("Persisting a recovery transition failed");
    protected readonly JobId _catchupJobId = JobId.New();
    protected readonly Key _retryablePartition = "retryable-partition";
    protected Exception? _clearanceError;
    protected bool _wasDisconnectedAfterFailure;
    protected bool _wasQuarantinedAfterFailure;
    protected bool _wasSubscribedAfterFailure;
    protected bool _owedRecoveryAfterFailure;
    protected Type? _stateAfterFailure;

    /// <summary>
    /// Gets the running state whose first persisted entry fails. Disconnected by default.
    /// </summary>
    protected virtual ObserverRunningState FailingEntry => ObserverRunningState.Disconnected;

    public an_observer_whose_quarantine_leave_fails_to_persist()
    {
        _silo.Options.StorageFactory = type => type == typeof(ObserverState) ? _faultableStateStorage : null!;
    }

    async Task Establish()
    {
        var failures = new FailedPartitions();
        failures.AddFailedPartition(_retryablePartition);
        _failedPartitionsStorage.State = failures;
        await _observer.TransitionTo<QuarantinedObserver>();

        var jobs = ImmutableList.Create(new JobState
        {
            Id = _catchupJobId,
            Status = JobStatus.Stopped,
            Request = new CatchUpObserverRequest(_observerKey, ObserverType.External, EventSequenceNumber.First, [EventType.Unknown])
        });
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult<IImmutableList<JobState>>(jobs));
        _jobsManager.ClearReceivedCalls();
        _appendedEventsQueues.ClearReceivedCalls();

        // Recovery from the Disconnected entry hook only schedules its onward transition, which the state machine
        // runs after persisting the entry - and drops if that write fails.
        _faultableStateStorage.FailNextWriteOf(FailingEntry, _writeFailure);
        _clearanceError = await Catch.Exception(_observer.ClearObserverQuarantine);
        _stateAfterFailure = (await _observer.GetCurrentState()).GetType();
        _wasDisconnectedAfterFailure = _stateAfterFailure == typeof(Disconnected);
        _wasQuarantinedAfterFailure = await _observer.IsObserverQuarantined();
        _wasSubscribedAfterFailure = await _observer.IsSubscribed();
        _owedRecoveryAfterFailure = _observer.OwesRecoveryAfterQuarantine();
    }
}
