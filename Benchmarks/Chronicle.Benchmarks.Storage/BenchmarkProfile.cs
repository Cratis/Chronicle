// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>
/// The sizes and iteration counts of one benchmark run. A profile bounds the run: nothing is measured
/// beyond what the profile names.
/// </summary>
/// <param name="Name">The profile name, `smoke` or `medium`.</param>
/// <param name="EventCounts">The number of seeded events for the catch-up and activation workloads.</param>
/// <param name="WarmupCount">The number of BenchmarkDotNet warmup iterations.</param>
/// <param name="IterationCount">The number of BenchmarkDotNet measured iterations.</param>
/// <param name="Concurrency">The writer counts for the concurrent append workloads.</param>
/// <param name="BatchSizes">The batch sizes for `AppendMany`.</param>
/// <param name="ConstraintBatchSizes">The governed batch sizes for the uniqueness read workload.</param>
/// <param name="PayloadBytes">The size of the event payload.</param>
/// <param name="TimeoutMinutes">The wall-clock budget of the whole run.</param>
/// <param name="RequiresConfirmation">Whether the run is long enough to need an explicit confirmation flag.</param>
public sealed record BenchmarkProfile(
    string Name,
    IReadOnlyList<int> EventCounts,
    int WarmupCount,
    int IterationCount,
    IReadOnlyList<int> Concurrency,
    IReadOnlyList<int> BatchSizes,
    IReadOnlyList<int> ConstraintBatchSizes,
    int PayloadBytes,
    int TimeoutMinutes,
    bool RequiresConfirmation)
{
    /// <summary>Gets the bounded profile: one thousand events, one warmup, three iterations, a few minutes.</summary>
    public static BenchmarkProfile Smoke { get; } = new("smoke", [1_000], 1, 3, [1, 8], [10, 100, 1_000], [1, 50, 500], 256, 20, false);

    /// <summary>Gets the on-demand profile: ten thousand and one million events. Long; never part of a gate.</summary>
    public static BenchmarkProfile Medium { get; } = new("medium", [10_000, 1_000_000], 1, 5, [1, 8, 32], [10, 100, 1_000], [1, 50, 500], 256, 240, true);

    /// <summary>Parses a profile name.</summary>
    /// <param name="name">The profile name.</param>
    /// <returns>The profile.</returns>
    /// <exception cref="ArgumentException">The name is not a known profile.</exception>
    public static BenchmarkProfile Parse(string name) => name.ToLowerInvariant() switch
    {
        "smoke" => Smoke,
        "medium" => Medium,
        _ => throw new ArgumentException($"Unknown profile '{name}'. Use smoke or medium.", nameof(name))
    };

    /// <summary>Gets a copy with a different payload size.</summary>
    /// <param name="payloadBytes">The payload size in bytes.</param>
    /// <returns>The adjusted profile.</returns>
    public BenchmarkProfile WithPayloadBytes(int payloadBytes) => this with { PayloadBytes = payloadBytes };
}
