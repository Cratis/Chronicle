// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_clearing_quarantine_after_automatic_reconciliation_with_pending_recovery : given.a_reactivated_quarantined_observer
{
    readonly JobId _catchupJobId = JobId.New();
    readonly JobId _retryJobId = JobId.New();
    readonly Key _inFlightPartition = "partition-in-flight";
    readonly Key _catchingUpPartition = "partition-catching-up";
    FailedPartition _failedPartition;
    FailedPartition _quarantinedPartition;
    bool _wasQuarantinedAfterReconciliation;

    async Task Establish()
    {
        var failedPartitions = new FailedPartitions();
        _failedPartition = failedPartitions.AddFailedPartition("failed-partition");
        _quarantinedPartition = failedPartitions.AddFailedPartition("quarantined-partition");
        failedPartitions.Quarantine(_quarantinedPartition.Partition);
        _failedPartitionsStorage.State = failedPartitions;
        _stateStorage.State = _stateStorage.State with
        {
            InFlightPartitions = new HashSet<Key>([_inFlightPartition, _catchingUpPartition]),
            CatchingUpPartitions = new HashSet<Key>([_catchingUpPartition]),
            FailedPartitionCount = 2
        };
        _jobsManager.GetAllJobs().Returns(ImmutableList.Create(
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
                Request = new RetryFailedPartitionRequest(_observerKey, ObserverType.External, _failedPartition.Partition, EventSequenceNumber.First, [EventType.Unknown])
            }));
        _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>())
            .Returns(Result<JobId, StartJobError>.Success(JobId.New()));
        await _observer.Subscribe<IEventStoreSubscriptionObserverSubscriber>(ObserverType.External, [EventType.Unknown], SiloAddress.Zero, "target", automatic: true);
        _wasQuarantinedAfterReconciliation = await _observer.IsObserverQuarantined();
        _jobsManager.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.ClearObserverQuarantine();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_wait_for_operator_clearance() => _wasQuarantinedAfterReconciliation.ShouldBeTrue();
    [Fact] void should_be_active() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_resume_the_stopped_catchup_job() => _jobsManager.Received(1).Resume(_catchupJobId);
    [Fact] void should_resume_the_stopped_retry_job() => _jobsManager.Received(1).Resume(_retryJobId);
    [Fact] void should_recover_the_retryable_failed_partition() => _jobsManager.Received(1)
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Is<RetryFailedPartitionRequest>(request => request.Key == _failedPartition.Partition));
    [Fact] void should_leave_the_individually_quarantined_partition_alone() => _jobsManager.DidNotReceive()
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Is<RetryFailedPartitionRequest>(request => request.Key == _quarantinedPartition.Partition));
    [Fact] void should_recover_the_in_flight_partition() => _jobsManager.Received(1)
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Is<CatchUpObserverPartitionRequest>(request => request.Key == _inFlightPartition));
    [Fact] void should_not_duplicate_existing_partition_catchup() => _jobsManager.DidNotReceive()
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Is<CatchUpObserverPartitionRequest>(request => request.Key == _catchingUpPartition));
}
