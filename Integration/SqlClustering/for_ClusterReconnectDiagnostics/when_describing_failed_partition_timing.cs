// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterReconnectDiagnostics;

public class when_describing_failed_partition_timing : Specification
{
    static readonly DateTimeOffset _occurred = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    string _description;

    void Because() => _description = ClusterReconnectDiagnostics.DescribeFailure(new FailedPartition(
        Guid.Empty,
        "reconnect",
        "partition",
        [new(_occurred, 42, ["delivery failed"], string.Empty)])
    {
        IsQuarantined = true
    });

    [Fact] void should_identify_the_failure() => _description.ShouldContain($"Id={Guid.Empty}");
    [Fact] void should_include_attempt_count() => _description.ShouldContain("Attempts=1");
    [Fact] void should_include_attempt_time() => _description.ShouldContain($"LastAttemptAt={_occurred:O}");
    [Fact] void should_not_invent_the_retry_time() => _description.ShouldContain("NextRetryAt=unavailable");
    [Fact] void should_include_quarantine_state() => _description.ShouldContain("IsQuarantined=True");
}
