// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_AllocationAttribution;

/// <summary>Paired allocation diagnostics over the real SQLite storage; no elapsed-time benchmark is run.</summary>
/// <param name="output">The specification output, including the allocation table.</param>
public class when_attributing_sqlite_allocations(ITestOutputHelper output) : Specification
{
    AllocationComparison[] _samples = [];

    void Because() => _samples = AllocationProbe.OnThread(Sample);

    [Fact]
    void should_report_boundaries_without_changing_the_data_or_command_count()
    {
        foreach (var sample in _samples)
        {
            sample.Write(output);

            // Cached-scope allocations have too little margin for a run-order-independent comparison.
            if (sample.Boundary != "SQLite IDatabase dispatch + context/scope (cached options both)")
            {
                (sample.Legacy > sample.Clean).ShouldBeTrue();
            }
        }
        _samples.Length.ShouldEqual(6);
    }

    static AllocationComparison[] Sample()
    {
        using var legacy = new SqlAllocationFixture(true);
        using var clean = new SqlAllocationFixture(false);
        legacy.Start();
        clean.Start();
        AllocationComparison[] samples =
        [
            AllocationComparison.Measure("SQLite namespace options only", legacy.BuildOptions, clean.CachedOptions),
            AllocationComparison.Measure("SQLite IDatabase dispatch + context/scope (cached options both)", legacy.CachedScope, clean.CachedScope),
            AllocationComparison.Measure("SQLite GetState inclusive", legacy.State, clean.State),
            AllocationComparison.Measure("SQLite GetTail inclusive", legacy.Tail, clean.Tail),
            AllocationComparison.Measure("SQLite GetTailPerEventType inclusive", legacy.TypeTails, clean.TypeTails),
            AllocationComparison.Measure("SQLite full drain inclusive", legacy.Drain, clean.Drain, SqlAllocationFixture.EventCount, 16)
        ];
        legacy.ValidateDrain();
        clean.ValidateDrain();
        legacy.Drained.ShouldEqual(SqlAllocationFixture.EventCount);
        clean.Drained.ShouldEqual(SqlAllocationFixture.EventCount);
        legacy.Checksum.ShouldEqual(32640UL);
        clean.Checksum.ShouldEqual(32640UL);

        // Each of the three 100/100/56-event pages queries events and named tags.
        legacy.DrainCommands.ShouldEqual(6L);
        clean.DrainCommands.ShouldEqual(legacy.DrainCommands);
        return samples;
    }
}
