// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Cratis.Chronicle.Benchmarks.ColdStart;

/// <summary>
/// Interval arithmetic and serializable descriptions for startup diagnostic records.
/// </summary>
internal static class StartupDiagnosticRecords
{
    /// <summary>
    /// Gets the JIT-related environment variables reported with the environment.
    /// </summary>
    static readonly string[] _jitVariables = ["DOTNET_TieredCompilation", "DOTNET_TieredPGO", "DOTNET_ReadyToRun", "COMPlus_TieredCompilation", "COMPlus_TieredPGO", "COMPlus_ReadyToRun"];

    /// <summary>
    /// Computes the total length covered by the union of the intervals.
    /// </summary>
    /// <param name="intervals">The intervals as start/end timestamps.</param>
    /// <returns>The covered ticks.</returns>
    /// <exception cref="InvalidOperationException">Thrown when an interval ends before it starts.</exception>
    internal static long Union(IEnumerable<(long Start, long End)> intervals)
    {
        var ordered = intervals.OrderBy(interval => interval.Start).ThenBy(interval => interval.End).ToArray();
        if (ordered.Any(interval => interval.End < interval.Start)) throw new InvalidOperationException("Negative diagnostic interval.");
        if (ordered.Length == 0) return 0;
        var start = ordered[0].Start;
        var end = ordered[0].End;
        long union = 0;
        foreach (var interval in ordered.Skip(1))
        {
            if (interval.Start > end)
            {
                union += end - start;
                start = interval.Start;
            }
            end = Math.Max(end, interval.End);
        }
        return union + end - start;
    }

    /// <summary>
    /// Describes a snapshot for serialization. Inclusive/category totals overlap; only interval unions are wall time, so never sum categories.
    /// </summary>
    /// <param name="snapshot">The snapshot, if instrumented.</param>
    /// <param name="start">The readiness start timestamp.</param>
    /// <param name="end">The readiness end timestamp.</param>
    /// <returns>The serializable description.</returns>
    internal static object Describe(StartupDiagnosticSnapshot? snapshot, long start, long end)
    {
        var records = snapshot?.Records ?? [];
        var valid = snapshot?.Valid ?? true;
        return new
        {
            ValidAttribution = valid,
            Capacity = snapshot?.Capacity ?? 0,
            Dropped = snapshot?.Dropped ?? 0,
            Active = snapshot?.Active ?? 0,
            TickFrequency = Stopwatch.Frequency,
            ReadinessStart = start,
            ReadinessEnd = end,
            ReadinessTicks = end - start,
            CoveredUnionTicks = valid ? Union(records.Select(record => (record.Start, record.End))) : (long?)null,
            Categories = records.GroupBy(record => record.Operation).Select(group => new
            {
                Operation = (int)group.Key,
                SpanCount = group.Count(),
                InclusiveTicks = group.Sum(record => record.End - record.Start),
                UnionTicks = valid ? Union(group.Select(record => (record.Start, record.End))) : (long?)null
            }).ToArray(),
            Records = records
        };
    }

    /// <summary>
    /// Describes the process environment for serialization.
    /// </summary>
    /// <returns>The serializable environment description.</returns>
    internal static object EnvironmentInfo() => new
    {
        Runtime = RuntimeInformation.FrameworkDescription,
        Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
        OS = RuntimeInformation.OSDescription,
        Environment.ProcessorCount,
        ServerGC = GCSettings.IsServerGC,
        LatencyMode = GCSettings.LatencyMode.ToString(),
        KernelImage = Environment.GetEnvironmentVariable("CRATIS_CHRONICLE_LOCAL_IMAGE"),
        LoadNote = Environment.GetEnvironmentVariable("COLDSTART_LOAD_NOTE") ?? "not supplied; machine load uncontrolled",
        Jit = _jitVariables
            .ToDictionary(name => name, name => Environment.GetEnvironmentVariable(name) ?? "runtime default"),
        HarnessMvid = typeof(StartupDiagnosticRecords).Assembly.ManifestModule.ModuleVersionId,
        ClientMvid = typeof(ChronicleClient).Assembly.ManifestModule.ModuleVersionId,
        HarnessSha256 = Hash(typeof(StartupDiagnosticRecords).Assembly.Location),
        ClientSha256 = Hash(typeof(ChronicleClient).Assembly.Location),
        SourceState = Environment.GetEnvironmentVariable("CHRONICLE_STARTUP_DIAGNOSTIC_SOURCE") ?? "unspecified dirty diagnostic checkout"
    };

    /// <summary>
    /// Computes the SHA-256 of a file.
    /// </summary>
    /// <param name="file">The file path.</param>
    /// <returns>The hexadecimal hash.</returns>
    internal static string Hash(string file) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));
}
