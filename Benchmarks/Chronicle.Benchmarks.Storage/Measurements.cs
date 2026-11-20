// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text.Json;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>Collects the <see cref="OperationStats"/> of every benchmark case in a run and writes them as `counts.json`.</summary>
public static class Measurements
{
    static readonly ConcurrentDictionary<string, OperationStats> _stats = new(StringComparer.Ordinal);

    /// <summary>Gets the stats for a case, creating them on first use.</summary>
    /// <param name="key">The case key: benchmark, provider and parameters.</param>
    /// <returns>The stats.</returns>
    public static OperationStats For(string key) => _stats.GetOrAdd(key, static _ => new());

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
}
