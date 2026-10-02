// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions.for_FailedPartitionConverters;

public class when_round_tripping_a_failed_partition_with_a_reset_retry_budget : Specification
{
    Concepts.Observation.FailedPartition _restored;

    void Establish()
    {
        var partition = new Concepts.Observation.FailedPartition { Partition = "failed-partition" };
        partition.AddAttempt(new FailedPartitionAttempt { SequenceNumber = 12UL });
        partition.AddAttempt(new FailedPartitionAttempt { SequenceNumber = 15UL });
        partition.AttemptsBeforeBudgetReset = 2;
        partition.AddAttempt(new FailedPartitionAttempt { SequenceNumber = 16UL });
        _restored = partition.ToEntity().ToFailedPartition();
    }

    [Fact] void should_restore_the_attempts_before_the_reset() => _restored.AttemptsBeforeBudgetReset.ShouldEqual(2);
    [Fact] void should_restore_the_attempts_in_the_current_budget() => _restored.AttemptsInCurrentBudget.ShouldEqual(1);
    [Fact] void should_keep_the_attempt_history() => _restored.Attempts.Count().ShouldEqual(3);
}
