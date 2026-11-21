// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_RunSummary;

[Collection(nameof(StorageRunOptionsCollection))]
public class when_appending_scaling_results : Specification
{
    string _summary = default!;
    void Because()
    {
        Measurements.Clear();
        Measurements.For("sqlite/catch-up-full/1000").Apply(new(1, 1000, new(1, new Dictionary<string, long>()), [TimeSpan.FromMilliseconds(10)], 0));
        Measurements.For("sqlite/catch-up-full/2000").Apply(new(1, 2000, new(1, new Dictionary<string, long>()), [TimeSpan.FromMilliseconds(20)], 0));
        _summary = RunSummary.ForProvider("sqlite", null);
    }
    void Destroy() => Measurements.Clear();
    [Fact] void should_add_a_separate_scaling_section() => _summary.ShouldContain("## Catch-up scaling");
    [Fact] void should_show_normalized_latency() => _summary.ShouldContain("| 2000 | 20 | 10 | 1 |  |");
    [Fact] void should_keep_six_operation_columns() => _summary.Split('\n').Single(line => line.StartsWith("| sqlite | catch-up-full/1000", StringComparison.Ordinal)).Count(character => character == '|').ShouldEqual(7);
}
