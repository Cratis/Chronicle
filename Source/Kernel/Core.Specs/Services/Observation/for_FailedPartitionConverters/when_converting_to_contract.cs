// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Services.Observation.for_FailedPartitionConverters;

public class when_converting_to_contract : Specification
{
    Concepts.Observation.FailedPartition _failedPartition;
    Contracts.Observation.FailedPartition _result;

    void Establish() =>
        _failedPartition = new()
        {
            Id = Concepts.Observation.FailedPartitionId.New(),
            ObserverId = "the-observer",
            Partition = new Key("the-partition", ArrayIndexers.NoIndexers),
            Attempts = [],
            IsResolved = true,
            IsQuarantined = true
        };

    void Because() => _result = _failedPartition.ToContract();

    [Fact] void should_set_is_resolved() => _result.IsResolved.ShouldBeTrue();
    [Fact] void should_set_is_quarantined() => _result.IsQuarantined.ShouldEqual(true);
}
