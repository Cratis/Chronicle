// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_receiving_reminder_for_failed_partition_and_observer_is_quarantined : given.an_observer_with_subscription
{
    const string Partition = "SomePartition";

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

    async Task Because() => await _observer.ReceiveReminder(Partition, default);

    [Fact]
    void should_not_start_recover_job() => _jobsManager.DidNotReceive()
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>());
}
