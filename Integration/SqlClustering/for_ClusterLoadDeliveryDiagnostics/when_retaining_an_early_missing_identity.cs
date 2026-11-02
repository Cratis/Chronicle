// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterLoadDeliveryDiagnostics;

public class when_retaining_an_early_missing_identity : Specification
{
    readonly ClusterLoadDeliveryDiagnostics _diagnostics = new();
    readonly Guid _id = Guid.NewGuid();
    readonly Guid _otherId = Guid.NewGuid();
    string _report = null!;

    void Because()
    {
        _diagnostics.Callback(_otherId, 2, () => { });
        _diagnostics.Callback(_id, 3, () => { });
        for (ulong sequence = 4; sequence < 10000; sequence++)
        {
            _diagnostics.Callback(Guid.NewGuid(), sequence, () => { });
        }
        _report = _diagnostics.Describe([(_id, 2UL), (_id, 8UL)], [(_otherId, 2UL)]);
    }

    [Fact] public void should_keep_early_identity_evidence_after_ring_wrap() => _report.Contains(_otherId.ToString(), StringComparison.Ordinal).ShouldBeTrue();
    [Fact] public void should_identify_same_sequence_other_id() => _report.Contains("same-sequence-other-id", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] public void should_identify_same_id_other_sequence() => _report.Contains("same-id-other-sequence=3", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] public void should_report_multiple_missing_candidates() => _report.Contains("missing=2", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] public void should_report_ring_eviction_not_delivery_absence() => _report.Contains("evicted=", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] public void should_never_exceed_the_output_budget() => (System.Text.Encoding.UTF8.GetByteCount(_report) <= 65536).ShouldBeTrue();
}
