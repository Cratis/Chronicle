// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// A manual partition retry is refused while the observer is quarantined, so clearing the observer quarantine is the
/// step that makes the retry possible. This pins the recovery order operators are told to follow: clear the observer
/// quarantine first, then retry the partition.
/// </summary>
public class when_recovering_a_failed_partition_after_clearing_observer_quarantine : given.an_observer_with_subscription
{
    const string Partition = "SomePartition";
    PartitionRecoveryOutcome _outcome;

    async Task Establish()
    {
        _failedPartitionsState.Partitions =
        [
            new()
            {
                Partition = (Key)Partition
            }
        ];
        await _observer.TransitionTo<QuarantinedObserver>();
        await _observer.ClearObserverQuarantine();
        _jobsManager.ClearReceivedCalls();
    }

    async Task Because() => _outcome = await _observer.TryStartRecoverJobForFailedPartition((Key)Partition);

    [Fact] void should_report_started() => _outcome.ShouldEqual(PartitionRecoveryOutcome.Started);

    [Fact] void should_start_a_recover_job_for_the_partition() => _jobsManager.Received(1)
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(
            Arg.Is<RetryFailedPartitionRequest>(_ =>
                _.ObserverKey == _observerKey &&
                _.Key == (Key)Partition));
}
