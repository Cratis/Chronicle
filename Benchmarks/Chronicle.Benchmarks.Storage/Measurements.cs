// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text.Json;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>
/// Collects the <see cref="OperationStats"/> of every benchmark case in a run and writes them as `counts.json`.
/// Every invocation is first held as pending; only <see cref="Commit"/> turns the last invocations of a case, the
/// ones BenchmarkDotNet ran as measured iterations, into counts. Warmup and jitting invocations never reach the stats.
/// </summary>
public static class Measurements
{
    static readonly ConcurrentDictionary<string, OperationStats> _stats = new(StringComparer.Ordinal);
    static readonly ConcurrentDictionary<string, List<InvocationRecord>> _pending = new(StringComparer.Ordinal);

    /// <summary>Gets the stats for a case, creating them on first use.</summary>
    /// <param name="key">The case key: benchmark, provider and parameters.</param>
    /// <returns>The stats.</returns>
    public static OperationStats For(string key) => _stats.GetOrAdd(key, static _ => new());

    /// <summary>Holds one invocation until the case is committed.</summary>
    /// <param name="key">The case key.</param>
    /// <param name="record">What the invocation did.</param>
    public static void Hold(string key, InvocationRecord record)
    {
        var list = _pending.GetOrAdd(key, static _ => []);
        lock (list) list.Add(record);
    }

    /// <summary>
    /// Counts the last invocations of every pending case and discards the rest. With the monitoring strategy
    /// BenchmarkDotNet runs one invocation per iteration and the measured iterations come last, after jitting and warmup.
    /// </summary>
    /// <param name="measuredIterations">How many measured iterations the profile runs.</param>
    public static void Commit(int measuredIterations)
    {
        foreach (var (key, list) in _pending.ToArray())
        {
            InvocationRecord[] measured;
            lock (list)
            {
                measured = [.. list.Skip(Math.Max(0, list.Count - measuredIterations))];
                list.Clear();
            }

            var stats = For(key);
            foreach (var record in measured) stats.Apply(record);
        }
    }

    /// <summary>Gets the number of failed operations counted so far.</summary>
    /// <returns>The error count.</returns>
    public static long TotalErrors() => _stats.Values.Sum(_ => _.Errors);

    /// <summary>Clears everything collected.</summary>
    public static void Clear()
    {
        _stats.Clear();
        _pending.Clear();
    }

    /// <summary>Writes everything collected as JSON.</summary>
    /// <param name="path">The file to write.</param>
    public static void Write(string path)
    {
        var report = _stats.OrderBy(_ => _.Key, StringComparer.Ordinal).ToDictionary(_ => _.Key, _ => _.Value.ToReport());
        File.WriteAllText(path, JsonSerializer.Serialize(report, SettingsLedger.JsonOptions));
    }

    /// <summary>Gets the keys collected so far.</summary>
    /// <returns>The keys.</returns>
    public static IReadOnlyCollection<string> Keys() => [.. _stats.Keys];
}
