// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// A manual recovery request against a quarantined observer used to silently report success while doing nothing -
/// automatic and manual recovery are paused alike until the quarantine is explicitly cleared, and the caller deserves
/// to know that is why nothing happened.
/// </summary>
public class when_recovering_a_failed_partition_and_the_observer_is_quarantined : given.an_observer_with_subscription
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
    }

    async Task Because() => _outcome = await _observer.TryStartRecoverJobForFailedPartition((Key)Partition);

    [Fact] void should_report_observer_quarantined() => _outcome.ShouldEqual(PartitionRecoveryOutcome.ObserverQuarantined);

    [Fact] void should_not_start_a_recover_job() => _jobsManager.DidNotReceive()
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>());
}
