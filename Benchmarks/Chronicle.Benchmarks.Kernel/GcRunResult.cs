// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Kernel;

/// <summary>
/// What one run of the GC workload measured. All sizes are bytes.
/// </summary>
/// <param name="ServerGc">Whether the run used Server GC.</param>
/// <param name="Operations">Observer subscribe and unsubscribe calls completed in the load window.</param>
/// <param name="LoadSeconds">The length of the load window.</param>
/// <param name="AllocatedBytes">Bytes allocated during the load window.</param>
/// <param name="PauseMilliseconds">Time the process was paused by garbage collection during the load window.</param>
/// <param name="Gen0Collections">Gen 0 collections during the load window.</param>
/// <param name="Gen1Collections">Gen 1 collections during the load window.</param>
/// <param name="Gen2Collections">Gen 2 collections during the load window.</param>
/// <param name="JsonOptionsAfterLoad">System.Text.Json options instances the runtime still tracked after the load window, before any forced collection; -1 when the runtime does not expose them.</param>
/// <param name="JsonOptionsLive">System.Text.Json options instances still tracked after the forced collections; -1 when unavailable.</param>
/// <param name="CommittedAfterSettling">Memory the collector had committed after settling, before any forced collection.</param>
/// <param name="ResidentAfterSettling">Working set of the process after settling, before any forced collection.</param>
/// <param name="HeapAfterSettling">Managed heap size after settling, before any forced collection.</param>
/// <param name="Gen2FragmentationAfterSettling">Gen 2 fragmentation after settling, as of the last collection.</param>
/// <param name="LiveAfterForcedCollection">Bytes live after forcing full compacting collections.</param>
/// <param name="CommittedAfterForcedCollection">Memory the collector had committed after the forced collections.</param>
public sealed record GcRunResult(
    bool ServerGc,
    long Operations,
    double LoadSeconds,
    long AllocatedBytes,
    double PauseMilliseconds,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections,
    int JsonOptionsAfterLoad,
    int JsonOptionsLive,
    long CommittedAfterSettling,
    long ResidentAfterSettling,
    long HeapAfterSettling,
    long Gen2FragmentationAfterSettling,
    long LiveAfterForcedCollection,
    long CommittedAfterForcedCollection)
{
    /// <summary>
    /// Gets the throughput of the load window, in operations per second.
    /// </summary>
    public double OperationsPerSecond => Operations / LoadSeconds;

    /// <summary>
    /// Gets the allocation rate of the load window, in bytes per second.
    /// </summary>
    public double AllocationRate => AllocatedBytes / LoadSeconds;
}
