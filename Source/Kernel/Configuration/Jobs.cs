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
    /// Defaults to 1 hour.
    /// </summary>
    public TimeSpan DeadJobThreshold { get; init; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets the cleanup cadence for the scavenger process that removes dead jobs.
    /// This determines how often the cleanup process runs to check for and remove jobs stuck in preparation.
    /// Defaults to 1 hour.
    /// </summary>
    public TimeSpan CleanupCadence { get; init; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets the effective maximum parallel steps to use.
    /// </summary>
    /// <returns>The maximum parallel steps value.</returns>
    public int GetEffectiveMaxParallelSteps() => MaxParallelSteps ?? Math.Max(1, Environment.ProcessorCount - 1);
}
