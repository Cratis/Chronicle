// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterLoadDiagnostics;

public class when_describing_a_failed_partition : Specification
{
    string _description = null!;

    void Because() => _description = ClusterLoadDiagnostics.DescribeFailedPartition(new FailedPartition(
        Guid.NewGuid(),
        "cluster-integrity",
        "partition",
        [new(DateTimeOffset.UtcNow, 2, ["earlier failure"], string.Empty), new(DateTimeOffset.UtcNow, 3, ["delivery failed"], string.Empty)])
    {
        IsQuarantined = true
    });

    [Fact] public void should_identify_the_partition() => _description.ShouldContain("Failed partition partition");
    [Fact] public void should_identify_the_observer() => _description.ShouldContain("ObserverId=cluster-integrity");
    [Fact] public void should_include_the_last_failed_sequence() => _description.ShouldContain("LastFailedSequenceNumber=3");
    [Fact] public void should_include_the_failure_message() => _description.ShouldContain("delivery failed");
    [Fact] public void should_include_the_quarantine_state() => _description.ShouldContain("IsQuarantined=True");
}
