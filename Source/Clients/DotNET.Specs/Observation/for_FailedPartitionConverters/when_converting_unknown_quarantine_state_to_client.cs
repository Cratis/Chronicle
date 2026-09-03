// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_FailedPartitionConverters;

public class when_converting_unknown_quarantine_state_to_client : Specification
{
    FailedPartition _result;

    void Because() =>
        _result = new Contracts.Observation.FailedPartition
        {
            Id = Guid.NewGuid(),
            ObserverId = "the-observer",
            Partition = "the-partition",
            Attempts = [],
            IsQuarantined = null
        }.ToClient();

    [Fact] void should_preserve_unknown_quarantine_state() => _result.IsQuarantined.ShouldBeNull();
}
