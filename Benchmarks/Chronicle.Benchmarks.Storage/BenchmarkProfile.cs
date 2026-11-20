// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>
/// The sizes and iteration counts of one benchmark run. A profile bounds the run: nothing is measured
/// beyond what the profile names.
/// </summary>
/// <param name="Name">The profile name, `smoke` or `medium`.</param>
/// <param name="Workload">The sizes the workloads are measured at.</param>
/// <param name="PayloadBytes">The size of the event payload.</param>
/// <param name="Plan">The iteration counts and the time budget.</param>
public sealed record BenchmarkProfile(string Name, WorkloadShape Workload, int PayloadBytes, RunPlan Plan)
{
    /// <summary>Gets the bounded profile: one thousand events, one warmup, three iterations, a few minutes.</summary>
    public static BenchmarkProfile Smoke { get; } = new("smoke", new([1_000], [1, 8], [10, 100, 1_000], [1, 50, 500]), 256, new(1, 3, 20, false));

    /// <summary>Gets the on-demand profile: ten thousand and one million events. Long; never part of a gate.</summary>
    public static BenchmarkProfile Medium { get; } = new("medium", new([10_000, 1_000_000], [1, 8, 32], [10, 100, 1_000], [1, 50, 500]), 256, new(1, 5, 240, true));

    /// <summary>Gets the number of seeded events for the catch-up and activation workloads.</summary>
    public IReadOnlyList<int> EventCounts => Workload.EventCounts;

    /// <summary>Gets the writer counts for the concurrent append workloads.</summary>
    public IReadOnlyList<int> Concurrency => Workload.Concurrency;

    /// <summary>Gets the batch sizes for `AppendMany`.</summary>
    public IReadOnlyList<int> BatchSizes => Workload.BatchSizes;

    /// <summary>Gets the governed batch sizes for the uniqueness read workload.</summary>
    public IReadOnlyList<int> ConstraintBatchSizes => Workload.ConstraintBatchSizes;

    /// <summary>Gets the number of BenchmarkDotNet warmup iterations.</summary>
    public int WarmupCount => Plan.WarmupCount;

    /// <summary>Gets the number of BenchmarkDotNet measured iterations.</summary>
    public int IterationCount => Plan.IterationCount;

    /// <summary>Gets the wall-clock budget of the whole run.</summary>
    public int TimeoutMinutes => Plan.TimeoutMinutes;

    /// <summary>Gets whether the run is long enough to need an explicit confirmation flag.</summary>
    public bool RequiresConfirmation => Plan.RequiresConfirmation;

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
