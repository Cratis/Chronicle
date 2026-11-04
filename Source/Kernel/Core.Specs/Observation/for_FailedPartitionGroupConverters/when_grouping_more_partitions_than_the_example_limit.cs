// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_FailedPartitionGroupConverters;

public class when_grouping_more_partitions_than_the_example_limit : Specification
{
    const int Total = FailedPartitionGroupConverters.MaximumExamples + 5;
    FailedPartitionGroup _result;

    void Because() => _result = Enumerable
        .Range(0, Total)
        .Select(index => FailedPartitions.With("observer", $"partition-{index}", (FailureKind.Handling, index)))
        .ToGroups()
        .Single();

    [Fact] void should_count_every_partition() => _result.PartitionCount.ShouldEqual(Total);
    [Fact] void should_cap_the_examples() => _result.ExamplePartitions.Count().ShouldEqual(FailedPartitionGroupConverters.MaximumExamples);
    [Fact] void should_list_the_most_recent_first() => _result.ExamplePartitions.First().ShouldEqual($"partition-{Total - 1}");
    [Fact] void should_leave_out_the_oldest() => _result.ExamplePartitions.ShouldNotContain("partition-0");
}
