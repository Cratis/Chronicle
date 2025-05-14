// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Extension methods for converting between contract and client job step progress representations.
/// </summary>
internal static class JobStepProgressConverters
{
    /// <summary>
    /// Converts a contract <see cref="Contracts.Jobs.JobStepProgress"/> to client <see cref="JobStepProgress"/>.
    /// </summary>
    /// <param name="progress">The contract <see cref="Contracts.Jobs.JobStepProgress"/>.</param>
    /// <returns>The client <see cref="JobStepProgress"/>.</returns>
    public static JobStepProgress ToClient(this Contracts.Jobs.JobStepProgress progress)
    {
        return new()
        {
            Percentage = progress.Percentage,
            Message = progress.Message
        };
    }

    /// <summary>
    /// Converts a collection of contract <see cref="Contracts.Jobs.JobStepProgress"/> to client <see cref="JobStepProgress"/>.
    /// </summary>
    /// <param name="progresses">The collection of contract <see cref="Contracts.Jobs.JobStepProgress"/>.</param>
    /// <returns>The collection of client <see cref="JobStepProgress"/>.</returns>
    public static IEnumerable<JobStepProgress> ToClient(this IEnumerable<Contracts.Jobs.JobStepProgress> progresses) =>
        progresses.Select(progress => progress.ToClient()).ToArray();
}
