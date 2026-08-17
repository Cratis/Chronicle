// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Concepts.Observation.for_FailedPartitions;

public class when_setting_partitions_from_two_observers_failing_on_the_same_partition : Specification
{
    static readonly Key _partition = new("c66fa6fd-95df-43f4-882a-a4fd380a9803", ArrayIndexers.NoIndexers);

    FailedPartitions _failedPartitions;
    FailedPartition _first;
    FailedPartition _second;
    Exception _error;

    void Establish()
    {
        _first = new() { Partition = _partition, ObserverId = "first-observer" };
        _second = new() { Partition = _partition, ObserverId = "second-observer" };
        _failedPartitions = new();
    }

    void Because() => _error = Catch.Exception(() => _failedPartitions.Partitions = [_first, _second]);

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_hold_both_entries() => _failedPartitions.Partitions.Count().ShouldEqual(2);
    [Fact] void should_report_the_partition_as_failed() => _failedPartitions.IsFailed(_partition).ShouldBeTrue();
}
