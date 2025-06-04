// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Extension methods for converting between <see cref="JobProgress"/> and <see cref="Contracts.Jobs.JobProgress"/>.
/// </summary>
internal static class JobProgressConverters
{
    /// <summary>
    /// Convert from <see cref="Contracts.Jobs.JobProgress"/> to <see cref="JobProgress"/>.
    /// </summary>
    /// <param name="jobProgress"><see cref="Contracts.Jobs.JobProgress"/> to convert from.</param>
    /// <returns>Converted <see cref="JobProgress"/>.</returns>
    public static JobProgress ToContract(this Contracts.Jobs.JobProgress jobProgress) =>
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
