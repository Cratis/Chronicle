// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_FailedPartitionGroupConverters;

public class when_grouping_by_the_latest_attempt_kind : Specification
{
    FailedPartitionGroup[] _result;

    void Because() => _result = [.. new[] { FailedPartitions.With("observer", "a", (FailureKind.Timeout, 1), (FailureKind.Handling, 2)) }.ToGroups()];

    [Fact] void should_have_a_single_group() => _result.Length.ShouldEqual(1);
    [Fact] void should_use_the_kind_of_the_latest_attempt() => _result[0].Kind.ShouldEqual(FailureKind.Handling);
}
