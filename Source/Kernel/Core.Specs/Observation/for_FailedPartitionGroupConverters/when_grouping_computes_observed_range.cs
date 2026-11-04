// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_FailedPartitionGroupConverters;

public class when_grouping_computes_observed_range : Specification
{
    FailedPartitionGroup _result;

    void Because() => _result = new[]
    {
        FailedPartitions.With("observer", "a", (FailureKind.Handling, 10), (FailureKind.Handling, 40)),
        FailedPartitions.With("observer", "b", (FailureKind.Handling, 5), (FailureKind.Handling, 20)),
        FailedPartitions.With("observer", "c")
    }.ToGroups().Single(_ => _.Kind == FailureKind.Handling);

    [Fact] void should_use_the_earliest_first_attempt() => _result.FirstObserved.ShouldEqual(FailedPartitions.Start.AddMinutes(5));
    [Fact] void should_use_the_latest_last_attempt() => _result.LastObserved.ShouldEqual(FailedPartitions.Start.AddMinutes(40));
}
