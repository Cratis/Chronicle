// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Concepts.Observation.for_FailedPartitions;

public class when_setting_partitions_with_an_exact_duplicate : Specification
{
    static readonly Key _partition = new("some-partition", ArrayIndexers.NoIndexers);

    FailedPartitions _failedPartitions;
    FailedPartition _older;
    FailedPartition _newer;

    void Establish()
    {
        _older = new() { Partition = _partition, ObserverId = "the-observer" };
        _newer = new() { Partition = _partition, ObserverId = "the-observer" };
        _failedPartitions = new();
    }

    void Because() => _failedPartitions.Partitions = [_older, _newer];

    [Fact] void should_collapse_to_a_single_entry() => _failedPartitions.Partitions.Count().ShouldEqual(1);
    [Fact] void should_keep_the_last_occurrence() => _failedPartitions.Partitions.Single().ShouldEqual(_newer);
}
