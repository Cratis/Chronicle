// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents the progress of a running observer job, in steps.
/// </summary>
/// <param name="HandledSteps">The number of steps that completed, successfully or with failures.</param>
/// <param name="TotalSteps">The number of steps of the job.</param>
public record ObserverJobGaugeProgress(int HandledSteps, int TotalSteps)
{
    /// <summary>
    /// The progress of a job that has not reported anything yet.
    /// </summary>
    public static readonly ObserverJobGaugeProgress None = new(0, 0);
}
