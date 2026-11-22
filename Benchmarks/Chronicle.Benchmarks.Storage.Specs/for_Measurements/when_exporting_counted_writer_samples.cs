// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_Measurements;

[Collection(nameof(StorageRunOptionsCollection))]
public class when_exporting_counted_writer_samples : Specification
{
    const string Key = "sqlite/append-concurrent-many-sequences/2";
    readonly string _samplesPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-samples.json");
    readonly string _countsPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-counts.json");
    JsonElement[] _samples = [];
    JsonElement _counts;

    void Establish() => Measurements.Clear();

    void Because()
    {
        var first = new InvocationRecord(2, 2, new(4, new Dictionary<string, long> { ["insert"] = 2, ["select"] = 2 }), [TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(2)], 0)
        {
            Samples = [new(0, 1, 0, 500, true), new(1, 2, 1, 2, true)]
        };
        Measurements.Record(Key, first);
        using (Measurements.Counting())
        {
            Measurements.Record(Key, first);
            Measurements.Record(Key, new(1, 1, new(3, new Dictionary<string, long> { ["insert"] = 1, ["select"] = 2 }), [TimeSpan.FromMilliseconds(3)], 1)
            {
                Samples = [new(0, 1, 0, 3, true), new(1, 2, 4, 9, false)]
            });
        }

        Measurements.Record(Key, first);
        Measurements.WriteSamples(_samplesPath);
        Measurements.Write(_countsPath);
        using var samples = JsonDocument.Parse(File.ReadAllText(_samplesPath));
        using var counts = JsonDocument.Parse(File.ReadAllText(_countsPath));
        _samples = samples.RootElement.EnumerateArray().Select(_ => _.Clone()).ToArray();
        _counts = counts.RootElement.GetProperty(Key).Clone();
    }

    void Destroy()
    {
        Measurements.Clear();
        File.Delete(_samplesPath);
        File.Delete(_countsPath);
    }

    [Fact] void should_export_only_counted_waves() => _samples.Length.ShouldEqual(4);
    [Fact] void should_identify_the_case() => _samples.Select(_ => _.GetProperty("caseKey").GetString()).ShouldEqual([Key, Key, Key, Key]);
    [Fact] void should_keep_wave_order() => _samples.Select(_ => _.GetProperty("waveIndex").GetInt32()).ShouldEqual([0, 0, 1, 1]);
    [Fact] void should_keep_writer_launch_order() => _samples.Select(_ => _.GetProperty("writerIndex").GetInt32()).ShouldEqual([0, 1, 0, 1]);
    [Fact] void should_identify_the_same_targets_across_waves() => _samples.Select(_ => _.GetProperty("sequenceIndex").GetInt32()).ShouldEqual([1, 2, 1, 2]);
    [Fact] void should_preserve_launch_offsets() => _samples.Select(_ => _.GetProperty("launchOffsetMs").GetDouble()).ShouldEqual([0d, 1d, 0d, 4d]);
    [Fact] void should_export_non_negative_launch_offsets() => _samples.ShouldEachConformTo(_ => _.GetProperty("launchOffsetMs").GetDouble() >= 0);
    [Fact] void should_not_sort_durations() => _samples.Select(_ => _.GetProperty("durationMs").GetDouble()).ShouldEqual([500d, 2d, 3d, 9d]);
    [Fact] void should_identify_failed_writers() => _samples.Select(_ => _.GetProperty("succeeded").GetBoolean()).ShouldEqual([true, true, true, false]);
    [Fact] void should_count_only_successful_operations() => _counts.GetProperty("operations").GetInt64().ShouldEqual(3L);
    [Fact] void should_count_only_successful_events() => _counts.GetProperty("events").GetInt64().ShouldEqual(3L);
    [Fact] void should_preserve_command_counts() => _counts.GetProperty("commands").GetInt64().ShouldEqual(7L);
    [Fact] void should_preserve_failure_counts() => _counts.GetProperty("errors").GetInt64().ShouldEqual(1L);
    [Fact] void should_keep_failure_duration_out_of_percentiles() => _counts.GetProperty("latencyMs").GetProperty("p50").GetDouble().ShouldEqual(3d);
}
