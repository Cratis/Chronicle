// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterLoadDiagnostics;

public class when_bounding_membership_diagnostics : Specification
{
    readonly ClusterLoadDiagnostics.MembershipDiagnostics _diagnostics = new();
    string _first = null!;
    string? _duplicate;
    string _replacement = null!;
    readonly List<string> _changes = [];

    void Because()
    {
        _first = _diagnostics.Record([new("node-a", 1, 3)])!;
        _duplicate = _diagnostics.Record([new("node-a", 1, 3)]);
        _replacement = _diagnostics.Record([new("node-a", 2, 3)])!;
        for (var generation = 3; generation < 50; generation++)
        {
            var result = _diagnostics.Record(Enumerable.Range(0, 15).Select(index => new MembershipEntry($"node-{index:D2}", generation, 3)).ToArray());
            if (result is not null)
            {
                _changes.Add(result);
            }
        }
    }

    [Fact] public void should_identify_the_original_incarnation() => _first.ShouldContain("node-a@1 status=3");
    [Fact] public void should_suppress_unchanged_polls() => _duplicate.ShouldBeNull();
    [Fact] public void should_expose_a_new_incarnation_at_the_same_address() => _replacement.ShouldContain("node-a@2 status=3");
    [Fact] public void should_bound_changes_to_forty_snapshots() => _changes.Count.ShouldEqual(38);
    [Fact] public void should_report_the_full_count() => _changes[0].ShouldContain("entries=15:");
    [Fact] public void should_include_the_twelfth_row() => _changes[0].ShouldContain("node-11@3");
    [Fact] public void should_bound_rows_per_snapshot() => _changes[0].ShouldNotContain("node-12");
}
