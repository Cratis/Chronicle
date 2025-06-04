// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Services.Jobs;

/// <summary>
/// Extension methods for converting between <see cref="JobStepState"/> and <see cref="JobStep"/>.
/// </summary>
internal static class JobStepConverters
{
    /// <summary>
    /// Convert from <see cref="JobStepState"/> to <see cref="JobStep"/>.
    /// </summary>
    /// <param name="jobStep"><see cref="JobStepState"/> to convert from.</param>
    /// <returns>Converted <see cref="JobStep"/>.</returns>
    public static JobStep ToContract(this JobStepState jobStep) =>
        new()
        {
            Id = jobStep.Id.JobStepId,
            Type = jobStep.Type,
            Name = jobStep.Name,
            Status = (JobStepStatus)(int)jobStep.Status,
            StatusChanges = jobStep.StatusChanges.ToContract(),
            Progress = jobStep.Progress.ToContract(),
        };

    /// <summary>
    /// Convert from <see cref="IEnumerable{JobStepState}"/> to <see cref="IEnumerable{JobStep}"/>.
    /// </summary>
    /// <param name="jobSteps">Collection of <see cref="JobStepState"/> to convert from.</param>
    /// <returns>Converted collection of <see cref="JobStep"/>.</returns>
    public static IEnumerable<JobStep> ToContract(this IEnumerable<JobStepState> jobSteps) =>
        jobSteps.Select(_ => _.ToContract()).ToArray();
}
