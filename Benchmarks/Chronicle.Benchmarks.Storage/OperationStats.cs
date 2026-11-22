// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Globalization;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>What was measured for one benchmark case: operations, events, commands and latency samples.</summary>
public sealed class OperationStats
{
    readonly ConcurrentDictionary<string, long> _byType = new(StringComparer.Ordinal);
    readonly List<double> _latenciesMs = [];
    readonly List<IReadOnlyList<WriterSample>> _waves = [];
    long _operations;
    long _events;
    long _commands;
    long _errors;

    /// <summary>Gets the number of operations.</summary>
    public long Operations => Interlocked.Read(ref _operations);

    /// <summary>Gets the number of events the operations carried or returned.</summary>
    public long Events => Interlocked.Read(ref _events);

    /// <summary>Gets the number of commands that reached the database.</summary>
    public long Commands => Interlocked.Read(ref _commands);

    /// <summary>Gets the number of operations that failed.</summary>
    public long Errors => Interlocked.Read(ref _errors);

    /// <summary>Gets the commands per operation.</summary>
    public double CommandsPerOperation => Operations == 0 ? 0 : (double)Commands / Operations;

    /// <summary>Gets the commands per one thousand events.</summary>
    public double CommandsPer1000Events => Events == 0 ? 0 : Commands * 1000.0 / Events;

    /// <summary>Computes a percentile of a set of samples by the nearest-rank method.</summary>
    /// <param name="samples">The samples.</param>
    /// <param name="percentile">The percentile, from 0 to 100.</param>
    /// <returns>The percentile, or 0 when there are no samples.</returns>
    public static double Percentile(IReadOnlyCollection<double> samples, double percentile)
    {
        if (samples.Count == 0) return 0;
        var ordered = samples.Order().ToArray();
        var rank = (int)Math.Ceiling(percentile / 100.0 * ordered.Length);
        return ordered[Math.Clamp(rank - 1, 0, ordered.Length - 1)];
    }

    /// <summary>Counts what one invocation did: successes as operations, events and latency, failures as errors.</summary>
    /// <param name="record">The invocation record.</param>
    public void Apply(InvocationRecord record)
    {
        if (record.Samples.Count > 0)
        {
            lock (_waves) _waves.Add(record.Samples);
        }

        if (record.Errors > 0) Interlocked.Add(ref _errors, record.Errors);
        if (record.Operations == 0) return;
        RecordBatch(record.Operations, record.Events, record.Commands);
        foreach (var latency in record.Latencies) RecordLatency(latency);
    }

    /// <summary>Records one finished operation.</summary>
    /// <param name="events">The events it carried or returned.</param>
    /// <param name="commands">The commands it caused.</param>
    /// <param name="elapsed">How long it took.</param>
    public void Record(long events, CommandSnapshot commands, TimeSpan elapsed)
    {
        Interlocked.Increment(ref _operations);
        Interlocked.Add(ref _events, events);
        AddCommands(commands);
        RecordLatency(elapsed);
    }

    /// <summary>Records a group of operations that ran together (for example concurrent writers), whose commands cannot be split per operation.</summary>
    /// <param name="operations">The number of operations in the group.</param>
    /// <param name="events">The events they carried or returned.</param>
    /// <param name="commands">The commands the whole group caused.</param>
    public void RecordBatch(long operations, long events, CommandSnapshot commands)
    {
        Interlocked.Add(ref _operations, operations);
        Interlocked.Add(ref _events, events);
        AddCommands(commands);
    }

    /// <summary>Records one latency sample without counting an operation (used for individual writers inside a concurrent operation).</summary>
    /// <param name="elapsed">The latency.</param>
    public void RecordLatency(TimeSpan elapsed)
    {
        lock (_latenciesMs) _latenciesMs.Add(elapsed.TotalMilliseconds);
    }

    /// <summary>Records a failed operation.</summary>
    public void RecordError() => Interlocked.Increment(ref _errors);

    /// <summary>Gets a JSON-ready view of the stats.</summary>
    /// <returns>The view.</returns>
    public object ToReport()
    {
        double[] samples;
        lock (_latenciesMs) samples = [.. _latenciesMs];
        var report = new Dictionary<string, object>
        {
            ["operations"] = Operations,
            ["events"] = Events,
            ["commands"] = Commands,
            ["errors"] = Errors,
            ["commandsPerOperation"] = Math.Round(CommandsPerOperation, 3),
            ["commandsPer1000Events"] = Math.Round(CommandsPer1000Events, 3),
            ["commandsByType"] = new SortedDictionary<string, long>(_byType),
            ["latencyMs"] = LatencyReport(samples)
        };
        var waves = WaveSnapshot();
        if (waves.Length > 0)
        {
            report["coldLatencyMs"] = LatencyReport(SuccessfulLatencies(waves.Take(1)));
            report["warmLatencyMs"] = LatencyReport(SuccessfulLatencies(waves.Skip(1)));
            report["launchSpreadMs"] = waves.Select((wave, waveIndex) => new { waveIndex, maxOffsetMs = wave.Max(_ => _.LaunchOffsetMs) }).ToArray();
        }

        return report;
    }

    /// <summary>Describes overall, cold and warm latency and each wave's launch spread, if samples exist.</summary>
    /// <returns>The additive markdown for a sampled case, otherwise an empty string.</returns>
    public string WaveSummary()
    {
        var waves = WaveSnapshot();
        if (waves.Length == 0) return string.Empty;
        double[] overall;
        lock (_latenciesMs) overall = [.. _latenciesMs];
        var spreads = waves.Select((wave, index) => string.Create(CultureInfo.InvariantCulture, $"wave {index}={wave.Max(_ => _.LaunchOffsetMs):0.###}"));
        return $"<br>Latency ms — overall: {DescribeLatency(overall)}; cold (wave 0): {DescribeLatency(SuccessfulLatencies(waves.Take(1)))}; warm (waves ≥1): {DescribeLatency(SuccessfulLatencies(waves.Skip(1)))}; launch spread ms (max offset): {string.Join(", ", spreads)}";
    }

    static double[] SuccessfulLatencies(IEnumerable<IReadOnlyList<WriterSample>> waves) => [.. waves.SelectMany(_ => _).Where(_ => _.Succeeded).Select(_ => _.DurationMs)];

    static object LatencyReport(double[] samples) => new { samples = samples.Length, p50 = Percentile(samples, 50), p99 = Percentile(samples, 99), max = samples.Length == 0 ? 0 : samples.Max() };

    static string DescribeLatency(double[] samples) => string.Create(CultureInfo.InvariantCulture, $"samples={samples.Length}, p50={Percentile(samples, 50):0.###}, p99={Percentile(samples, 99):0.###}, max={(samples.Length == 0 ? 0 : samples.Max()):0.###}");

    IReadOnlyList<WriterSample>[] WaveSnapshot()
    {
        lock (_waves) return [.. _waves];
    }

    void AddCommands(CommandSnapshot commands)
    {
        Interlocked.Add(ref _commands, commands.Total);
        foreach (var (type, count) in commands.ByType) _byType.AddOrUpdate(type, static (_, added) => added, static (_, current, added) => current + added, count);
    }
}
