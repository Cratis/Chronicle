// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_clearing_partition_quarantine;

public class and_the_partition_is_not_found : given.an_observer_with_subscription
{
    ClearPartitionQuarantineResult _result;

    async Task Because() => _result = await _observer.ClearPartitionQuarantine((Key)"Unknown", true);

    [Fact] void should_report_not_found() => _result.Outcome.ShouldEqual(ClearPartitionQuarantineOutcome.NotFound);
    [Fact] void should_not_write_to_storage() => _failedPartitionsStorageStats.Writes.ShouldEqual(0);
    [Fact] void should_not_start_a_recover_job() => _jobsManager.DidNotReceive()
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>());
}
