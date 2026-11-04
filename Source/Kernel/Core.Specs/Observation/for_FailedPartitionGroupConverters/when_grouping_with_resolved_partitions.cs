// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_FailedPartitionGroupConverters;

public class when_grouping_with_resolved_partitions : Specification
{
    FailedPartitionGroup[] _result;

    void Because()
    {
        var resolved = FailedPartitions.With("observer", "resolved", (FailureKind.Handling, 1));
        resolved.IsResolved = true;
        var current = FailedPartitions.With("observer", "current", (FailureKind.Handling, 2));
        _result = [.. new[] { resolved, current }.ToGroups()];
    }

    [Fact] void should_count_only_current_failures() => _result.Single().PartitionCount.ShouldEqual(1);
    [Fact] void should_only_list_the_current_failure() => _result.Single().ExamplePartitions.ShouldContainOnly("current");
}
