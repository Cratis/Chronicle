// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

using FailedPartitionContract = Cratis.Chronicle.Contracts.Observation.FailedPartition;

namespace Cratis.Chronicle.Observation.for_FailedPartitionContract;

public class when_round_tripping_quarantine_state_through_protobuf : Specification
{
    FailedPartitionContract _unknown;
    FailedPartitionContract _failed;
    FailedPartitionContract _quarantined;

    void Because()
    {
        _unknown = Serializer.DeepClone(new FailedPartitionContract { IsQuarantined = null });
        _failed = Serializer.DeepClone(new FailedPartitionContract { IsQuarantined = false });
        _quarantined = Serializer.DeepClone(new FailedPartitionContract { IsQuarantined = true });
    }

    [Fact] void should_preserve_unknown_state() => _unknown.IsQuarantined.ShouldBeNull();
    [Fact] void should_preserve_failed_state() => _failed.IsQuarantined.ShouldEqual(false);
    [Fact] void should_preserve_quarantined_state() => _quarantined.IsQuarantined.ShouldEqual(true);
}
