// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_FailedPartitionDetailsConverters;

public class when_converting_partition_status : Specification
{
    FailedPartitionDetails _result;

    void Because() => _result = new Concepts.Observation.FailedPartition { IsQuarantined = true, IsResolved = true }.ToReadModel();

    [Fact] void should_preserve_quarantine() => _result.IsQuarantined.ShouldEqual(true);
    [Fact] void should_preserve_resolution() => _result.IsResolved.ShouldBeTrue();
}
