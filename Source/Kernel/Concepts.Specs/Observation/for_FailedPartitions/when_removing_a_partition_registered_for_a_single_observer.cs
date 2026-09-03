// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Concepts.Observation.for_FailedPartitions;

public class when_removing_a_partition_registered_for_a_single_observer : Specification
{
    static readonly Key _partition = new("some-partition", ArrayIndexers.NoIndexers);

    FailedPartitions _failedPartitions;

    void Establish()
    {
        _failedPartitions = new();
        _failedPartitions.RegisterAttempt(_partition, EventSequenceNumber.First, ["something went wrong"], string.Empty);
    }

    void Because() => _failedPartitions.Remove(_partition);

    [Fact] void should_no_longer_report_the_partition_as_failed() => _failedPartitions.IsFailed(_partition).ShouldBeFalse();
    [Fact] void should_have_no_failed_partitions() => _failedPartitions.HasFailedPartitions.ShouldBeFalse();
    [Fact] void should_track_the_partition_as_resolved() => _failedPartitions.ResolvedPartitions.Single().Partition.ShouldEqual(_partition);
    [Fact] void should_mark_the_partition_as_resolved() => _failedPartitions.ResolvedPartitions.Single().IsResolved.ShouldBeTrue();
}
