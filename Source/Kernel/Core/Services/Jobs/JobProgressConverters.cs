// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;

namespace Cratis.Chronicle.Services.Jobs;

/// <summary>
/// Extension methods for converting between <see cref="JobProgress"/> and <see cref="Concepts.Jobs.JobProgress"/>.
/// </summary>
internal static class JobProgressConverters
{
    /// <summary>
    /// Convert from <see cref="Concepts.Jobs.JobProgress"/> to <see cref="JobProgress"/>.
    /// </summary>
    /// <param name="jobProgress"><see cref="Concepts.Jobs.JobProgress"/> to convert from.</param>
    /// <returns>Converted <see cref="JobProgress"/>.</returns>
    public static JobProgress ToContract(this Concepts.Jobs.JobProgress jobProgress) =>
        new()
        {
            TotalSteps = jobProgress.TotalSteps,
            SuccessfulSteps = jobProgress.SuccessfulSteps,
            FailedSteps = jobProgress.FailedSteps,
            StoppedSteps = jobProgress.StoppedSteps,
            IsCompleted = jobProgress.IsCompleted,
            IsStopped = jobProgress.IsStopped,
            Message = jobProgress.Message
        };
}
