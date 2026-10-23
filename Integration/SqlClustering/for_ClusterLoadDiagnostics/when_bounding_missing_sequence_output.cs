// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterLoadDiagnostics;

public class when_bounding_missing_sequence_output : Specification
{
    string _description = null!;

    void Because() => _description = ClusterLoadDiagnostics.DescribeMissingSequences(
        Enumerable.Range(1, 60).Reverse().Select(sequence => (Guid.NewGuid(), (EventSequenceNumber)(ulong)sequence)),
        []);

    [Fact] public void should_report_the_total_missing_count() => _description.ShouldContain("missing stored event sequence numbers (60, first 50):");
    [Fact] public void should_print_missing_sequences_in_order() => _description.ShouldContain("1, 2, 3, 4, 5");
    [Fact] public void should_stop_after_fifty_missing_sequences() => _description.EndsWith("48, 49, 50", StringComparison.Ordinal).ShouldBeTrue();
}
