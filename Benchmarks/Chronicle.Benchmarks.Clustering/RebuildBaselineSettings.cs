// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>Validated dimensions of a baseline run.</summary>
/// <param name="Scenario">The Scenario measurement input.</param>
/// <param name="Mode">The Mode measurement input.</param>
/// <param name="Size">The Size measurement input.</param>
/// <param name="Seed">The Seed measurement input.</param>
/// <param name="Topology">The Topology measurement input.</param>
/// <param name="LiveRate">The LiveRate measurement input.</param>
/// <param name="TimeoutSeconds">The TimeoutSeconds measurement input.</param>
internal sealed record RebuildBaselineSettings(string Scenario, string Mode, int Size, int Seed, string Topology, int LiveRate, int TimeoutSeconds)
{
    /// <summary>Gets the selected physical cluster shape.</summary>
    internal ClusterTopology Cluster => ParseTopology(Topology);

    /// <summary>
    /// Maps a topology name to its physical cluster shape.
    /// </summary>
    /// <param name="topology">The topology name from the run settings.</param>
    /// <returns>The selected cluster topology.</returns>
    /// <exception cref="ArgumentException">The topology name is unsupported.</exception>
    internal static ClusterTopology ParseTopology(string topology) => topology switch
    {
        "single" => ClusterTopology.SingleSilo,
        "two" => ClusterTopology.TwoSilos,
        "split-jobs" => ClusterTopology.TwoSilosWithSplitJobs,
        "three" => ClusterTopology.ThreeSilos,
        _ => throw new ArgumentException("REBUILD_TOPOLOGY must be single, two, split-jobs or three.")
    };

    /// <summary>Reads the scenario and environment settings.</summary>
    /// <param name="scenario">The scenario measurement input.</param>
    /// <returns>The validated baseline settings.</returns>
    /// <exception cref="ArgumentException">The scenario, mode or topology is unsupported, or a numeric setting is not positive.</exception>
    internal static RebuildBaselineSettings Read(string scenario)
    {
        if (scenario is not ("S1" or "S2" or "S3" or "S4")) throw new ArgumentException("--rebuild requires S1, S2, S3 or S4.");
        var mode = Environment.GetEnvironmentVariable("REBUILD_MODE") ?? "live";
        var topology = Environment.GetEnvironmentVariable("REBUILD_TOPOLOGY") ?? "single";
        if (mode is not ("cold" or "live")) throw new ArgumentException("REBUILD_MODE must be cold or live.");
        ParseTopology(topology);
        var size = Environment.GetEnvironmentVariable("REBUILD_SIZE") ?? "small";
        var count = size switch { "small" => 10_000, "medium" => 1_000_000, "large" => 10_000_000, _ => Positive(size, "REBUILD_SIZE") };
        return new(scenario, mode, count, Integer("REBUILD_SEED", 5156), topology, Integer("REBUILD_LIVE_RATE", 200), Integer("REBUILD_TIMEOUT_SECONDS", 1200));
    }

    static int Integer(string name, int fallback) => Environment.GetEnvironmentVariable(name) is { } value ? Positive(value, name) : fallback;
    static int Positive(string value, string name) => int.TryParse(value, out var result) && result > 0 ? result : throw new ArgumentException($"{name} must be a positive integer.");
}
