// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_OperationStats.when_reporting_wave_latencies;

public class with_counted_waves : Specification
{
    JsonElement _report;

    void Because()
    {
        var stats = new OperationStats();
        stats.Apply(new(2, 2, new(4, new Dictionary<string, long>()), [TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200)], 1)
        {
            Samples = [new(0, 1, 2, 100, true), new(1, 2, 5, 200, true), new(2, 3, 9, 999, false)]
        });
        stats.Apply(new(2, 2, new(4, new Dictionary<string, long>()), [TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(2)], 0)
        {
            Samples = [new(0, 1, 1, 1, true), new(1, 2, 3, 2, true)]
        });
        stats.Apply(new(1, 1, new(2, new Dictionary<string, long>()), [TimeSpan.FromMilliseconds(3)], 0)
        {
            Samples = [new(0, 1, 4, 3, true)]
        });
        _report = JsonSerializer.SerializeToElement(stats.ToReport(), SettingsLedger.JsonOptions);
    }

    [Fact] void should_preserve_overall_counts() => _report.GetProperty("operations").GetInt64().ShouldEqual(5L);
    [Fact] void should_preserve_errors() => _report.GetProperty("errors").GetInt64().ShouldEqual(1L);
    [Fact] void should_preserve_overall_latencies() => JsonElement.DeepEquals(_report.GetProperty("latencyMs"), JsonSerializer.SerializeToElement(new { samples = 5, p50 = 3d, p99 = 200d, max = 200d })).ShouldBeTrue();
    [Fact] void should_report_only_successful_cold_latencies() => JsonElement.DeepEquals(_report.GetProperty("coldLatencyMs"), JsonSerializer.SerializeToElement(new { samples = 2, p50 = 100d, p99 = 200d, max = 200d })).ShouldBeTrue();
    [Fact] void should_report_all_successful_warm_latencies() => JsonElement.DeepEquals(_report.GetProperty("warmLatencyMs"), JsonSerializer.SerializeToElement(new { samples = 3, p50 = 2d, p99 = 3d, max = 3d })).ShouldBeTrue();
    [Fact] void should_identify_each_wave() => _report.GetProperty("launchSpreadMs").EnumerateArray().Select(_ => _.GetProperty("waveIndex").GetInt32()).ShouldEqual([0, 1, 2]);
    [Fact] void should_include_failed_writers_in_each_maximum_launch_offset() => _report.GetProperty("launchSpreadMs").EnumerateArray().Select(_ => _.GetProperty("maxOffsetMs").GetDouble()).ShouldEqual([9d, 3d, 4d]);
}
