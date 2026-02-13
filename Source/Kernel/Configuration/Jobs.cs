// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents configuration for jobs.
/// </summary>
public class Jobs
{
    /// <summary>
    /// Gets the maximum number of parallel job steps that can be executed concurrently.
    /// </summary>
    /// <remarks>
    /// If not configured, defaults to the number of processor threads minus 1, but never less than 1.
    /// </remarks>
    public int? MaxParallelSteps { get; init; }

    /// <summary>
    /// Gets the time threshold for considering a job as dead in the water.
    /// Jobs in preparation state with no steps that were created before this threshold are candidates for cleanup.
    /// </summary>
    /// <remarks>
    /// If not configured, defaults to 1 hour.
    /// </remarks>
    public TimeSpan? DeadJobThreshold { get; init; }

    /// <summary>
    /// Gets the cleanup cadence for the scavenger process.
    /// </summary>
    /// <remarks>
    /// If not configured, defaults to 1 hour.
    /// </remarks>
    public TimeSpan? CleanupCadence { get; init; }

    /// <summary>
    /// Gets the effective maximum parallel steps to use.
    /// </summary>
    /// <returns>The maximum parallel steps value.</returns>
    public int GetEffectiveMaxParallelSteps() => MaxParallelSteps ?? Math.Max(1, Environment.ProcessorCount - 1);

    /// <summary>
    /// Gets the effective dead job threshold to use.
    /// </summary>
    /// <returns>The dead job threshold value.</returns>
    public TimeSpan GetEffectiveDeadJobThreshold() => DeadJobThreshold ?? TimeSpan.FromHours(1);

    /// <summary>
    /// Gets the effective cleanup cadence to use.
    /// </summary>
    /// <returns>The cleanup cadence value.</returns>
    public TimeSpan GetEffectiveCleanupCadence() => CleanupCadence ?? TimeSpan.FromHours(1);
}
