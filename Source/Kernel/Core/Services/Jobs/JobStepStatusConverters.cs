// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Services.Jobs;

/// <summary>
/// Extension methods for converting between <see cref="JobStepState"/> and <see cref="JobStep"/>.
/// </summary>
internal static class JobStepStatusConverters
{
    /// <summary>
    /// Convert from <see cref="JobStepState"/> to <see cref="JobStepStatusChanged"/>.
    /// </summary>
    /// <param name="jobStepStatus"><see cref="JobStepState"/> to convert from.</param>
    /// <returns>Converted <see cref="JobStepStatusChanged"/>.</returns>
    public static JobStepStatusChanged ToContract(this Concepts.Jobs.JobStepStatusChanged jobStepStatus) =>
        new()
        {
            Status = (JobStepStatus)(int)jobStepStatus.Status,
            Occurred = jobStepStatus.Occurred!,
            ExceptionMessages = jobStepStatus.ExceptionMessages,
            ExceptionStackTrace = jobStepStatus.ExceptionStackTrace,
        };

    /// <summary>
    /// Convert from <see cref="IEnumerable{JobStepState}"/> to <see cref="IEnumerable{JobStepStatusChanged}"/>.
    /// </summary>
    /// <param name="jobStepStatuses">Collection of <see cref="JobStepState"/> to convert from.</param>
    /// <returns>Converted collection of <see cref="JobStepStatusChanged"/>.</returns>
    public static IEnumerable<JobStepStatusChanged> ToContract(this IEnumerable<Concepts.Jobs.JobStepStatusChanged> jobStepStatuses) =>
        jobStepStatuses.Select(_ => _.ToContract()).ToArray();
}
