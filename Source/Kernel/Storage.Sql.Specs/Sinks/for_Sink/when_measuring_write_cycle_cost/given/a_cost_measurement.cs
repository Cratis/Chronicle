// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Dynamic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_measuring_write_cycle_cost.given;

/// <summary>
/// Applies a number of events of every kind of write cycle and records commands, transactions and elapsed time per kind. It
/// uses only the sink's public contract, so the same measurement runs against the code before and after a change.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class a_cost_measurement<THarness> : a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    readonly List<Measurement> _measurements = [];
    ulong _sequence = 1000;

    /// <summary>
    /// Gets the name of the database the leaf runs on.
    /// </summary>
    protected abstract string Database { get; }

    /// <summary>
    /// Gets the measured kinds of write cycle.
    /// </summary>
    protected IReadOnlyList<Kind> Kinds =>
    [
        new("a_direct_insert", false, (_, _) => Task.CompletedTask, (tag, i) => Apply($"row-{tag}-{i}", Next(), Set(("group", "first")))),
        new("a_direct_update", false, (tag, _) => Apply($"row-{tag}", 1, Set(("group", "first"))), (tag, i) => Apply($"row-{tag}", Next(), Set(("group", $"v{i}")))),
        new("a_single_join", false, (tag, _) => Apply($"target-{tag}", 1, Set(("joinId", $"J-{tag}"))), (tag, i) => Apply($"trigger-{tag}", Next(), KeyedFromJoin("joinId", $"J-{tag}", ("x", i)))),
        new("a_single_child_removal", false, AddChildren, (tag, i) => Apply($"row-{tag}", Next(), RemoveChildFromAll($"c{i}"))),
        new("a_removal", false, AddRows, (tag, i) => Apply($"row-{tag}-{i}", Next(), new Removed(new ExpandoObject()))),
        new("a_guarded_direct_update", false, (tag, _) => Apply($"row-{tag}", 1, Set(("group", "first"))), (tag, i) => Apply($"row-{tag}", Next(), SinkWriteMode.GuardedUpsert, Set(("group", $"v{i}")))),
        new("a_join_and_a_direct_write", true, (tag, _) => Apply($"target-{tag}", 1, Set(("joinId", $"J-{tag}"))), (tag, i) => Apply($"source-{tag}", Next(), KeyedFromJoin("joinId", $"J-{tag}", ("x", i)), Set(("group", $"v{i}")))),
        new("two_joins", true, AddTwoTargets, (tag, i) => Apply($"source-{tag}", Next(), KeyedFromJoin("joinId", $"A-{tag}", ("x", i)), KeyedFromJoin("joinId", $"B-{tag}", ("x", i)))),
        new("a_join_a_child_removal_and_a_direct_write", true, AddTargetAndChildren, (tag, i) => Apply($"row-{tag}", Next(), KeyedFromJoin("joinId", $"J-{tag}", ("x", i)), RemoveChildFromAll($"c{i}"), Set(("group", $"v{i}")))),
        new("a_guarded_join_and_a_direct_write", true, (tag, _) => Apply($"target-{tag}", 1, Set(("joinId", $"J-{tag}"))), (tag, i) => Apply($"source-{tag}", Next(), SinkWriteMode.GuardedUpsert, KeyedFromJoin("joinId", $"J-{tag}", ("x", i)), Set(("group", $"v{i}"))))
    ];

    /// <summary>
    /// Gets the measurements taken so far.
    /// </summary>
    protected IReadOnlyList<Measurement> Measurements => _measurements;

    /// <summary>
    /// Gets the number of events applied per kind and run.
    /// </summary>
    protected static int Events => int.Parse(Environment.GetEnvironmentVariable("CHRONICLE_SINK_CYCLE_EVENTS") ?? "20");

    /// <summary>
    /// Gets the number of runs.
    /// </summary>
    protected static int Runs => int.Parse(Environment.GetEnvironmentVariable("CHRONICLE_SINK_CYCLE_RUNS") ?? "1");

    /// <summary>
    /// Measures every kind, after a warm-up of each.
    /// </summary>
    /// <returns>A task for the async work.</returns>
    protected async Task MeasureEveryKind()
    {
        var events = Events;
        var report = Environment.GetEnvironmentVariable("CHRONICLE_SINK_CYCLE_REPORT");
        foreach (var kind in Kinds)
        {
            // Warm up: table creation, migration cache, first prune.
            await kind.Prepare("warm", 1);
            await kind.Write("warm", 0);
        }

        for (var run = 1; run <= Runs; run++)
        {
            foreach (var kind in Kinds)
            {
                var tag = $"{kind.Name}{run}";
                await kind.Prepare(tag, events);
                _capture.Clear();
                var latencies = new List<double>(events);
                var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                var stopwatch = Stopwatch.StartNew();
                for (var i = 0; i < events; i++)
                {
                    var started = Stopwatch.GetTimestamp();
                    await kind.Write(tag, i);
                    latencies.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
                }

                stopwatch.Stop();
                var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
                latencies.Sort();
                var measurement = new Measurement(kind.Name, kind.Multi, run, events, _capture.Commands.Count, _capture.Started, _capture.Committed, stopwatch.Elapsed.TotalMilliseconds, _capture.Fingerprint, Percentile(latencies, 0.5), Percentile(latencies, 0.95), latencies.Count == 0 ? 0 : latencies[^1], allocated, events == 0 ? 0 : (double)allocated / events);
                _measurements.Add(measurement);
                if (report is not null)
                {
                    await WriteReport(report, measurement);
                }
            }
        }
    }

    async Task WriteReport(string report, Measurement measurement)
    {
        var operations = _capture.Operations;
        await using var inspection = OpenInspectionContext();
        var rows = await inspection.Entries.AsNoTracking().ToListAsync();
        var snapshot = JsonSerializer.Serialize(rows.OrderBy(row => row["id"]?.ToString(), StringComparer.Ordinal).Select(row => row.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToDictionary(pair => pair.Key, pair => pair.Value)));
        var result = new
        {
            database = Database,
            measurement.Kind,
            measurement.Multi,
            measurement.Run,
            measurement.Events,
            measurement.Commands,
            measurement.Transactions,
            measurement.Committed,
            measurement.ElapsedMilliseconds,
            measurement.Fingerprint,
            measurement.LatencyP50Milliseconds,
            measurement.LatencyP95Milliseconds,
            measurement.LatencyMaxMilliseconds,
            measurement.AllocatedBytes,
            measurement.AllocatedBytesPerEvent,
            RowFingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot))),
            Rows = snapshot,
            Operations = operations,
            NetworkTrips = (int?)null,
            TripEvidence = "Unknown: async EF boundaries are not wire/server evidence; BEGIN may be deferred, readers may stream, opens may be pooled. COMMIT and post-resolution cleanup are included in Operations, not inferred as trips. Transaction disposal is not exposed by the EF interceptor."
        };
        await File.AppendAllTextAsync(report, JsonSerializer.Serialize(result) + "\n");
    }

    static double Percentile(List<double> sorted, double fraction) =>
        sorted.Count == 0 ? 0 : sorted[Math.Min(sorted.Count - 1, (int)Math.Ceiling(fraction * sorted.Count) - 1)];

    ulong Next() => _sequence++;

    async Task AddRows(string tag, int count)
    {
        for (var i = 0; i < count; i++)
        {
            await Apply($"row-{tag}-{i}", 1, Set(("group", "first")));
        }
    }

    async Task AddTwoTargets(string tag, int count)
    {
        await Apply($"target-a-{tag}", 1, Set(("joinId", $"A-{tag}")));
        await Apply($"target-b-{tag}", 1, Set(("joinId", $"B-{tag}")));
    }

    async Task AddTargetAndChildren(string tag, int count)
    {
        await Apply($"target-{tag}", 1, Set(("joinId", $"J-{tag}")));
        await AddChildren(tag, count);
    }

    async Task AddChildren(string tag, int count)
    {
        for (var i = 0; i < count; i++)
        {
            await Apply($"row-{tag}", 1, AddChild($"c{i}"));
        }
    }

    /// <summary>
    /// A measured kind of write cycle.
    /// </summary>
    /// <param name="Name">The name.</param>
    /// <param name="Multi">Whether the cycle has several writes.</param>
    /// <param name="Prepare">Prepares the rows for a number of events.</param>
    /// <param name="Write">Applies one event.</param>
    protected sealed record Kind(string Name, bool Multi, Func<string, int, Task> Prepare, Func<string, int, Task> Write);

    /// <summary>
    /// One measurement.
    /// </summary>
    /// <param name="Kind">The kind.</param>
    /// <param name="Multi">Whether the cycle has several writes.</param>
    /// <param name="Run">The run.</param>
    /// <param name="Events">The number of events.</param>
    /// <param name="Commands">The number of commands.</param>
    /// <param name="Transactions">The number of transactions started.</param>
    /// <param name="Committed">The number of transactions committed.</param>
    /// <param name="ElapsedMilliseconds">The elapsed time.</param>
    /// <param name="Fingerprint">The command fingerprint.</param>
    /// <param name="LatencyP50Milliseconds">The median duration of one ApplyChanges.</param>
    /// <param name="LatencyP95Milliseconds">The 95th percentile duration of one ApplyChanges.</param>
    /// <param name="LatencyMaxMilliseconds">The longest duration of one ApplyChanges.</param>
    /// <param name="AllocatedBytes">The bytes allocated by the current thread across the timed section.</param>
    /// <param name="AllocatedBytesPerEvent">The allocated bytes per event.</param>
    protected sealed record Measurement(string Kind, bool Multi, int Run, int Events, int Commands, int Transactions, int Committed, double ElapsedMilliseconds, string Fingerprint, double LatencyP50Milliseconds, double LatencyP95Milliseconds, double LatencyMaxMilliseconds, long AllocatedBytes, double AllocatedBytesPerEvent);
}
