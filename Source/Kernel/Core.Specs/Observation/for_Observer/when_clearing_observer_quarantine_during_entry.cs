// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_clearing_observer_quarantine_during_entry : given.an_observer_with_subscription
{
    readonly TaskCompletionSource _cleanupEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<IImmutableList<JobState>> _cleanupJobs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly JobId _catchupJobId = JobId.New();
    readonly JobId _retryJobId = JobId.New();
    readonly Key _recoveredPartition = "recovered-partition";
    readonly Key _retryablePartition = "retryable-partition";
    IImmutableList<JobState> _jobs;
    Task _quarantineEntry;
    bool _wasQuarantinedAfterClearance;

    async Task Establish()
    {
        var failures = new FailedPartitions();
        failures.AddFailedPartition(_retryablePartition);
        _failedPartitionsStorage.State = failures;
        _configurationProvider.GetFor(Arg.Any<string>()).Returns(new Observers { QuarantineOnFailedPartitionCount = 1 });
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>(), (EventSourceId)_recoveredPartition.ToString())
            .Returns(GetSequenceNumberError.StorageError);
        _jobs = ImmutableList.Create(
            new JobState
            {
                Id = _catchupJobId,
                Status = JobStatus.Stopped,
                Request = new CatchUpObserverRequest(_observerKey, ObserverType.External, EventSequenceNumber.First, [EventType.Unknown])
            },
            new JobState
            {
                Id = _retryJobId,
                Status = JobStatus.Stopped,
                Request = new RetryFailedPartitionRequest(_observerKey, ObserverType.External, _retryablePartition, EventSequenceNumber.First, [EventType.Unknown])
            });
        _jobsManager.GetAllJobs().Returns(_ => _cleanupEntered.TrySetResult() ? _cleanupJobs.Task : Task.FromResult(_jobs));
        _jobsManager.ClearReceivedCalls();
        _appendedEventsQueues.ClearReceivedCalls();

        // FailedPartitionRecovered is AlwaysInterleave: clearance can run while its quarantine entry awaits cleanup.
        _quarantineEntry = _observer.FailedPartitionRecovered(_recoveredPartition, 42UL);
        await _cleanupEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        try
        {
            await _observer.ClearObserverQuarantine().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            _wasQuarantinedAfterClearance = await _observer.IsObserverQuarantined();
        }
        finally
        {
            _cleanupJobs.SetResult(_jobs);
        }
        await _quarantineEntry.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_defer_recovery_until_quarantine_cleanup_finishes() => _wasQuarantinedAfterClearance.ShouldBeTrue();
    [Fact] async Task should_be_observing() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] void should_persist_the_active_state() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] async Task should_keep_the_subscription() => (await _observer.IsSubscribed()).ShouldBeTrue();
    [Fact] async Task should_leave_quarantine() => (await _observer.IsObserverQuarantined()).ShouldBeFalse();
    [Fact] void should_subscribe_to_the_queue() => _appendedEventsQueues.Received(1).Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());
    [Fact] void should_resume_the_stopped_catchup_job() => _jobsManager.Received(1).Resume(_catchupJobId);
    [Fact] void should_resume_the_stopped_retry_job() => _jobsManager.Received(1).Resume(_retryJobId);
    [Fact] void should_retry_the_failed_partition() => _jobsManager.Received(1).Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Is<RetryFailedPartitionRequest>(request => request.Key == _retryablePartition));
}
