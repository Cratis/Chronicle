// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterLoadDeliveryDiagnostics;

public class when_capture_saturates : Specification
{
    readonly ClusterLoadDeliveryDiagnostics _diagnostics = new();
    readonly Guid _id = Guid.NewGuid();
    string _report = null!;

    void Because()
    {
        Parallel.For(0, 18000, sequence => _diagnostics.Callback(_id, (ulong)sequence, () => { }));
        _report = _diagnostics.Describe([(_id, 20000UL)], []);
    }

    [Fact] public void should_cap_distinct_sequence_summaries() => _diagnostics.SequenceCount.ShouldEqual(16384);
    [Fact] public void should_cap_the_recent_ring() => _diagnostics.RetainedRecords.ShouldEqual(4096);
    [Fact] public void should_report_unknown_coverage_when_saturated() => _report.Contains("coverage=unknown", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] public void should_bound_retained_storage() => (_diagnostics.StorageUpperBound <= 16 * 1024 * 1024).ShouldBeTrue();
    [Fact] public void should_bound_failure_output() => (System.Text.Encoding.UTF8.GetByteCount(_report) <= 65536).ShouldBeTrue();
}
