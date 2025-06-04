// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Extension methods for converting between contract and client job step status changed representations.
/// </summary>
internal static class JobStepStatusChangedConverters
{
    /// <summary>
    /// Converts a contract <see cref="Contracts.Jobs.JobStepStatusChanged"/> to client <see cref="JobStepStatusChanged"/>.
    /// </summary>
    /// <param name="statusChanged">The contract <see cref="Contracts.Jobs.JobStepStatusChanged"/>.</param>
    /// <returns>The client <see cref="JobStepStatusChanged"/>.</returns>
    public static JobStepStatusChanged ToClient(this Contracts.Jobs.JobStepStatusChanged statusChanged)
    {
        return new()
        {
            Status = (JobStepStatus)(int)statusChanged.Status,
            Occurred = statusChanged.Occurred,
            ExceptionMessages = statusChanged.ExceptionMessages,
            ExceptionStackTrace = statusChanged.ExceptionStackTrace,
        };
    }

    /// <summary>
    /// Converts a collection of contract <see cref="Contracts.Jobs.JobStepStatusChanged"/> to client <see cref="JobStepStatusChanged"/>.
    /// </summary>
    /// <param name="statusChanges">The collection of contract <see cref="Contracts.Jobs.JobStepStatusChanged"/>.</param>
    /// <returns>The collection of client <see cref="JobStepStatusChanged"/>.</returns>
    public static IEnumerable<JobStepStatusChanged> ToClient(this IEnumerable<Contracts.Jobs.JobStepStatusChanged> statusChanges) =>
        statusChanges.Select(statusChange => statusChange.ToClient()).ToArray();
}
