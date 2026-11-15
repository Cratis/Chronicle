// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_an_operator_retry_of_an_exhausted_partition_fails : given.a_quarantined_partition
{
    async Task Because()
    {
        await _observer.TryStartRecoverJobForFailedPartition((Key)Partition);
        await _observer.PartitionFailed(Partition, LastFailedSequenceNumber, ["operator retry failed"], string.Empty, FailureKind.Handling);
    }

    [Fact] void should_record_another_attempt() => FailedPartition.AttemptsInCurrentBudget.ShouldEqual(4);
    [Fact] void should_record_the_new_failure() => FailedPartition.Attempts.Last().Messages.ShouldContain("operator retry failed");
    [Fact] void should_exhaust_the_automatic_budget_again() => FailedPartition.IsQuarantined.ShouldBeTrue();
    [Fact] void should_persist_the_attempt() => _failedPartitionsStorageStats.Writes.ShouldEqual(2);
}
