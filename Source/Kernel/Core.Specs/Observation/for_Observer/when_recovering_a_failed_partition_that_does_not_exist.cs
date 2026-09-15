// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// A manual recovery request naming a partition the observer does not know as failed used to silently report
/// success. It must instead say plainly that there was nothing to recover, so an operator retrying a stale or
/// mistyped partition key is not told the retry worked when it did nothing.
/// </summary>
public class when_recovering_a_failed_partition_that_does_not_exist : given.an_observer_with_subscription
{
    PartitionRecoveryOutcome _outcome;

    async Task Because() => _outcome = await _observer.TryStartRecoverJobForFailedPartition((Key)"SomeUnknownPartition");

    [Fact] void should_report_partition_not_found() => _outcome.ShouldEqual(PartitionRecoveryOutcome.PartitionNotFound);

    [Fact] void should_not_start_a_recover_job() => _jobsManager.DidNotReceive()
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>());
}
