// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Extension methods for converting between contract and client job step representations.
/// </summary>
public static class JobStepConverters
{
    /// <summary>
    /// Converts a contract <see cref="Contracts.Jobs.JobStep"/> client <see cref="JobStep"/>.
    /// </summary>
    /// <param name="jobStep">The contract <see cref="Contracts.Jobs.JobStep"/> .</param>
    /// <returns>The client <see cref="JobStep"/>.</returns>
    public static JobStep ToClient(this Contracts.Jobs.JobStep jobStep)
    {
        return new()
        {
            Id = jobStep.Id,
            Type = jobStep.Type,
            Name = jobStep.Name,
            Status = (JobStepStatus)(int)jobStep.Status,
            StatusChanges = jobStep.StatusChanges.ToClient(),
            Progress = jobStep.Progress.ToClient(),
        };
    }

    /// <summary>
    /// Converts a collection of contract <see cref="Contracts.Jobs.JobStep"/> to client <see cref="JobStep"/>.
    /// </summary>
    /// <param name="jobSteps">The collection of contract <see cref="Contracts.Jobs.JobStep"/>.</param>
    /// <returns>The collection of client <see cref="JobStep"/>.</returns>
    public static IEnumerable<JobStep> ToClient(this IEnumerable<Contracts.Jobs.JobStep> jobSteps) =>
        jobSteps.Select(jobStep => jobStep.ToClient()).ToArray();
}
