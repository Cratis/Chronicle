// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// A manual recovery request for a partition that is genuinely failed and not quarantined must actually attempt
/// recovery and report that it did - the honest counterpart to the outcomes that report why nothing happened.
/// </summary>
public class when_recovering_a_failed_partition_that_is_healthy : given.an_observer_with_subscription
{
    const string Partition = "SomePartition";
    PartitionRecoveryOutcome _outcome;

    void Establish() => _failedPartitionsState.Partitions =
    [
        new()
        {
            Partition = (Key)Partition
        }
    ];

    async Task Because() => _outcome = await _observer.TryStartRecoverJobForFailedPartition((Key)Partition);

    [Fact] void should_report_started() => _outcome.ShouldEqual(PartitionRecoveryOutcome.Started);

    [Fact] void should_start_a_recover_job() => _jobsManager.Received(1)
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(
            Arg.Is<RetryFailedPartitionRequest>(_ =>
                _.ObserverKey == _observerKey &&
                _.Key == (Key)Partition));
}
