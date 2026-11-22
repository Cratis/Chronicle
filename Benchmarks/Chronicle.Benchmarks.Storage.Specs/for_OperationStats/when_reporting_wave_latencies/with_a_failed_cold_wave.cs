// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_OperationStats.when_reporting_wave_latencies;

public class with_a_failed_cold_wave : Specification
{
    JsonElement _report;
    JsonElement _coldOnly;

    void Because()
    {
        var stats = new OperationStats();
        stats.Apply(InvocationRecord.Failure with { Samples = [new(0, 1, 7, 100, false)] });
        _coldOnly = JsonSerializer.SerializeToElement(stats.ToReport(), SettingsLedger.JsonOptions);
        stats.Apply(new(1, 1, new(1, new Dictionary<string, long>()), [TimeSpan.FromMilliseconds(2)], 0)
        {
            Samples = [new(0, 1, 3, 2, true)]
        });
        _report = JsonSerializer.SerializeToElement(stats.ToReport(), SettingsLedger.JsonOptions);
    }

    [Fact] void should_keep_the_cold_group_empty() => _report.GetProperty("coldLatencyMs").GetProperty("samples").GetInt32().ShouldEqual(0);
    [Fact] void should_not_reclassify_the_next_wave_as_cold() => _report.GetProperty("warmLatencyMs").GetProperty("p99").GetDouble().ShouldEqual(2d);
    [Fact] void should_keep_failed_wave_launch_spread() => _report.GetProperty("launchSpreadMs")[0].GetProperty("maxOffsetMs").GetDouble().ShouldEqual(7d);
    [Fact] void should_report_zero_percentiles_for_an_empty_group() => _coldOnly.GetProperty("coldLatencyMs").GetProperty("p99").GetDouble().ShouldEqual(0d);
    [Fact] void should_report_zero_warm_samples_before_a_second_wave() => _coldOnly.GetProperty("warmLatencyMs").GetProperty("samples").GetInt32().ShouldEqual(0);
}
