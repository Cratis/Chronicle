// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the configuration of how the kernel keeps its memory use in check.
/// </summary>
public class MemoryManagement
{
    /// <summary>
    /// Gets whether work with a large footprint waits for room in the memory budget before it runs.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets the share of the memory available to the process, from 1 to 100, that is budgeted for work with a large footprint.
    /// </summary>
    /// <remarks>
    /// The memory available is the heap hard limit or the container limit when there is one, otherwise the memory of
    /// the machine. The rest is left for everything that is not gated: the runtime, caches, connections, and the
    /// buffers of work too small to wait.
    /// </remarks>
    public int BudgetPercentOfAvailableMemory { get; init; } = 40;

    /// <summary>
    /// Gets the share of the budget, from 1 to 100, that remains while the garbage collector reports a high memory load.
    /// </summary>
    public int BudgetPercentUnderPressure { get; init; } = 50;

    /// <summary>
    /// Gets the time in seconds a reservation waits for room before it is let through regardless. Zero or less waits indefinitely.
    /// </summary>
    /// <remarks>
    /// Memory is held for as long as the work holding it runs, which for a reducer includes the time the client takes to
    /// answer. This keeps one stuck holder from stopping all other work.
    /// </remarks>
    public int MaximumWaitSeconds { get; init; } = 120;

    /// <summary>
    /// Gets the estimated size in bytes below which work does not reserve memory at all.
    /// </summary>
    public long MinimumReservationBytes { get; init; } = 1024 * 1024;

    /// <summary>
    /// Gets how many times the size of a reducer state its reduce is expected to hold at its peak.
    /// </summary>
    /// <remarks>
    /// A reduce holds the stored state, the state sent to the client, the state it returns and the changes written,
    /// in several representations at once.
    /// </remarks>
    public int ReducerCostMultiplier { get; init; } = 4;
}
