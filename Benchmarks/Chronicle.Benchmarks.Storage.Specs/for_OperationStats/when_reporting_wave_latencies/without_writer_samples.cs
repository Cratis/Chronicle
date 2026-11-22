// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_OperationStats.when_reporting_wave_latencies;

public class without_writer_samples : Specification
{
    JsonElement _report;
    string _summary = default!;

    void Because()
    {
        var stats = new OperationStats();
        stats.Apply(new(1, 2, new(3, new Dictionary<string, long> { ["insert"] = 3 }), [TimeSpan.FromMilliseconds(4)], 0));
        _report = JsonSerializer.SerializeToElement(stats.ToReport(), SettingsLedger.JsonOptions);
        _summary = stats.WaveSummary();
    }

    [Fact] void should_keep_exactly_the_existing_fields() => _report.EnumerateObject().Select(_ => _.Name).ShouldEqual(["operations", "events", "commands", "errors", "commandsPerOperation", "commandsPer1000Events", "commandsByType", "latencyMs"]);
    [Fact] void should_keep_the_existing_latency() => _report.GetProperty("latencyMs").GetProperty("p99").GetDouble().ShouldEqual(4d);
    [Fact] void should_not_add_a_wave_summary() => _summary.ShouldEqual(string.Empty);
}
