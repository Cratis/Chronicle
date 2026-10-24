// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterLoad;

public class when_describing_payload_ids : Specification
{
    readonly Guid _acknowledged = new("00000000-0000-0000-0000-000000000001");
    readonly Guid _unknown = new("00000000-0000-0000-0000-000000000002");
    readonly Guid _unexpected = new("00000000-0000-0000-0000-000000000003");

    [Fact]
    public void should_identify_unexpected_stored_and_missing_acknowledged_payloads() =>
        ClusterLoad.DescribePayloadIds([_unknown, _unexpected], [_acknowledged, _unknown], [_acknowledged])
            .ShouldEqual($"Unexpected stored payload IDs (1): {_unexpected}; missing acknowledged payload IDs (1): {_acknowledged}");

    [Fact]
    public void should_allow_unknown_payloads_to_be_absent() =>
        ClusterLoad.DescribePayloadIds([_acknowledged], [_acknowledged, _unknown], [_acknowledged])
            .ShouldEqual("Unexpected stored payload IDs (0): ; missing acknowledged payload IDs (0): ");

    [Fact]
    public void should_allow_unknown_payloads_to_be_stored() =>
        ClusterLoad.DescribePayloadIds([_acknowledged, _unknown], [_acknowledged, _unknown], [_acknowledged])
            .ShouldEqual("Unexpected stored payload IDs (0): ; missing acknowledged payload IDs (0): ");
}
