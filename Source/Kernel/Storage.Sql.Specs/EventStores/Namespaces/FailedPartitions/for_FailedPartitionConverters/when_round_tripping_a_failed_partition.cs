// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions.for_FailedPartitionConverters;

public class when_round_tripping_a_failed_partition : Specification
{
    Concepts.Observation.FailedPartition _restored;
    DateTimeOffset _occurred;

    void Establish()
    {
        _occurred = new DateTimeOffset(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);
        var partition = new Concepts.Observation.FailedPartition { Partition = "failed-partition" };
        partition.AddAttempt(new FailedPartitionAttempt { SequenceNumber = 12UL, Occurred = _occurred });
        partition.AddAttempt(new FailedPartitionAttempt { SequenceNumber = 15UL, Occurred = _occurred.AddMinutes(1) });
        _restored = partition.ToEntity().ToFailedPartition();
    }

    [Fact] void should_restore_the_last_attempt_sequence_number() => _restored.LastAttempt.SequenceNumber.ShouldEqual((Concepts.Events.EventSequenceNumber)15UL);
    [Fact] void should_restore_the_last_attempt_time() => _restored.LastAttempt.Occurred.ShouldEqual(_occurred.AddMinutes(1));
}
