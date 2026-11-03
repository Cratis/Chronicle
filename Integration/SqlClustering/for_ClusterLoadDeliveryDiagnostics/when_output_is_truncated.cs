// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterLoadDeliveryDiagnostics;

public class when_output_is_truncated : Specification
{
    readonly ClusterLoadDeliveryDiagnostics _diagnostics = new();
    readonly List<(Guid Id, ulong Sequence)> _stored = [];
    string _report = null!;

    void Because()
    {
        _diagnostics.Record(new(1, Guid.NewGuid()));
        for (ulong sequence = 0; sequence < 100; sequence++)
        {
            _stored.Add((Guid.NewGuid(), sequence));
            for (var incarnation = 0; incarnation < 3; incarnation++)
            {
                var stream = Guid.NewGuid();
                _diagnostics.Record(new(5, stream, 1, Count: 1));
                _diagnostics.Record(new(6, stream, 1, Sequence: sequence));
                _diagnostics.Record(new(8, stream, 1, Sequence: sequence));
                _diagnostics.Record(new(9, stream, 1, Sequence: sequence));
                _diagnostics.Callback(Guid.NewGuid(), sequence, () => { });
            }
        }
        _report = _diagnostics.Describe(_stored, []);
    }

    void Destroy() => _diagnostics.Dispose();

    [Fact] public void should_include_the_header_even_when_full() => _report.StartsWith("delivery diagnostics:", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] public void should_report_all_missing_identities() => _report.Contains("missing=100", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] public void should_mark_omitted_candidate_details() => _report.Contains("candidate-details-omitted=50", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] public void should_report_identity_variant_overflow_as_unknown() => _report.Contains("history-coverage=unknown", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] public void should_bound_utf8_output_including_markers() => (System.Text.Encoding.UTF8.GetByteCount(_report) <= 65536).ShouldBeTrue();
    [Fact] public void should_include_an_explicit_truncation_marker() => _report.Contains("truncation:", StringComparison.Ordinal).ShouldBeTrue();
}
