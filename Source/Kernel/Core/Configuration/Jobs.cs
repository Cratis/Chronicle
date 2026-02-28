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
    /// Gets the effective maximum parallel steps to use.
    /// </summary>
    /// <returns>The maximum parallel steps value.</returns>
    public int GetEffectiveMaxParallelSteps() => MaxParallelSteps ?? Math.Max(1, Environment.ProcessorCount - 1);
}
