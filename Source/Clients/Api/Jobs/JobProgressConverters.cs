// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Jobs;

/// <summary>
/// Converters between <see cref="JobProgress"/> to a <see cref="JobProgress"/> for the API.
/// </summary>
internal static class JobProgressConverters
{
    /// <summary>
    /// Converts a <see cref="Contracts.Jobs.JobProgress"/> to a <see cref="JobProgress"/> for the API.
    /// </summary>
    /// <param name="jobProgress">The job progress to convert.</param>
    /// <returns>The converted job progress.</returns>
    public static JobProgress ToApi(this Contracts.Jobs.JobProgress jobProgress) => new(
        jobProgress.TotalSteps,
        jobProgress.SuccessfulSteps,
        jobProgress.FailedSteps,
        jobProgress.StoppedSteps,
        jobProgress.IsCompleted,
        jobProgress.IsStopped,
        jobProgress.Message);
}
