// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Observation.for_FailedPartitionConverters;

public class when_converting_to_api : Specification
{
    Contracts.Observation.FailedPartition _failedPartition;
    FailedPartition _result;

    void Establish() =>
        _failedPartition = new()
        {
            Id = Guid.NewGuid(),
            ObserverId = "the-observer",
            Partition = "the-partition",
            Attempts = [],
            IsResolved = true,
            IsQuarantined = true
        };

    void Because() => _result = _failedPartition.ToApi();

    [Fact] void should_set_is_resolved() => _result.IsResolved.ShouldBeTrue();
    [Fact] void should_set_is_quarantined() => _result.IsQuarantined.ShouldEqual(true);
}
