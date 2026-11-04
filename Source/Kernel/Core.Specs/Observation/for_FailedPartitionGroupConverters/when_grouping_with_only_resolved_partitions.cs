// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_FailedPartitionGroupConverters;

public class when_grouping_with_only_resolved_partitions : Specification
{
    IEnumerable<FailedPartitionGroup> _result;

    void Because()
    {
        var resolved = FailedPartitions.With("observer", "resolved", (FailureKind.Handling, 1));
        resolved.IsResolved = true;
        _result = new[] { resolved }.ToGroups();
    }

    [Fact] void should_have_no_groups() => _result.ShouldBeEmpty();
}
