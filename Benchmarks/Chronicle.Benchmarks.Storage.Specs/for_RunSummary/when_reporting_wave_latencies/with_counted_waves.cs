// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_RunSummary.when_reporting_wave_latencies;

[Collection(nameof(StorageRunOptionsCollection))]
public class with_counted_waves : Specification
{
    string _summary = default!;

    void Establish()
    {
        Measurements.Clear();
        var stats = Measurements.For("sqlite/append-concurrent-many-sequences/2");
        stats.Apply(new(1, 1, new(1, new Dictionary<string, long>()), [TimeSpan.FromMilliseconds(100)], 0)
        {
            Samples = [new(0, 1, 5, 100, true)]
        });
        stats.Apply(new(1, 1, new(1, new Dictionary<string, long>()), [TimeSpan.FromMilliseconds(2)], 0)
        {
            Samples = [new(0, 1, 3, 2, true)]
        });
        Measurements.For("sqlite/append/1").Record(1, new(1, new Dictionary<string, long>()), TimeSpan.FromMilliseconds(4));
    }

    void Because() => _summary = RunSummary.ForProvider("sqlite");
    void Destroy() => Measurements.Clear();

    [Fact] void should_show_overall_percentiles() => _summary.ShouldContain("overall: samples=2, p50=2, p99=100, max=100");
    [Fact] void should_show_cold_percentiles() => _summary.ShouldContain("cold (wave 0): samples=1, p50=100, p99=100, max=100");
    [Fact] void should_show_warm_percentiles() => _summary.ShouldContain("warm (waves ≥1): samples=1, p50=2, p99=2, max=2");
    [Fact] void should_show_launch_spread_per_wave() => _summary.ShouldContain("launch spread ms (max offset): wave 0=5, wave 1=3");
    [Fact] void should_preserve_unsampled_rows() => _summary.ShouldContain("| sqlite | append/1 | 1 | 1 | 1000 | 0 |");
    [Fact] void should_preserve_the_operation_columns() => _summary.Split('\n').Single(_ => _.Contains("append-concurrent-many-sequences", StringComparison.Ordinal)).Count(_ => _ == '|').ShouldEqual(7);
}
