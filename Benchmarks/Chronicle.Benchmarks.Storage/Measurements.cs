// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text.Json;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>
/// Collects the <see cref="OperationStats"/> of every benchmark case in a run and writes them as `counts.json`.
/// Invocations are only counted inside a <see cref="Counting"/> scope, which only the counted pass opens (see
/// <see cref="CountedPass"/>). BenchmarkDotNet's own invocations (jitting, pilot, warmup, actual) run outside any
/// scope, so they can never reach the stats.
/// </summary>
public static class Measurements
{
    static readonly ConcurrentDictionary<string, OperationStats> _stats = new(StringComparer.Ordinal);
    static volatile bool _counting;

    /// <summary>Gets a value indicating whether invocations are currently counted.</summary>
    public static bool IsCounting => _counting;

    /// <summary>Gets the stats for a case, creating them on first use.</summary>
    /// <param name="key">The case key: benchmark, provider and parameters.</param>
    /// <returns>The stats.</returns>
    public static OperationStats For(string key) => _stats.GetOrAdd(key, static _ => new());

    /// <summary>Opens a scope in which invocations are counted.</summary>
    /// <returns>The scope; dispose it to stop counting.</returns>
    public static IDisposable Counting()
    {
        _counting = true;
        return new CountingScope();
    }

    /// <summary>Counts one invocation, when a <see cref="Counting"/> scope is open. Otherwise it is ignored.</summary>
    /// <param name="key">The case key.</param>
    /// <param name="record">What the invocation did.</param>
    public static void Record(string key, InvocationRecord record)
    {
        if (_counting) For(key).Apply(record);
    }

    /// <summary>Gets the number of failed operations counted so far.</summary>
    /// <returns>The error count.</returns>
    public static long TotalErrors() => _stats.Values.Sum(_ => _.Errors);

    /// <summary>Clears everything collected.</summary>
    public static void Clear() => _stats.Clear();

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

    sealed class CountingScope : IDisposable
    {
        public void Dispose() => _counting = false;
    }
}
