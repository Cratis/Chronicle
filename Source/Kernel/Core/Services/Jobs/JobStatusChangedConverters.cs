// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;

namespace Cratis.Chronicle.Services.Jobs;

/// <summary>
/// Extension methods for converting between <see cref="Concepts.Jobs.JobProgress"/> and <see cref="JobProgress"/>.
/// </summary>
internal static class JobStatusChangedConverters
{
    /// <summary>
    /// Convert from <see cref="Concepts.Jobs.JobStatusChanged"/> to <see cref="JobStatusChanged"/>.
    /// </summary>
    /// <param name="statusChanged"><see cref="Concepts.Jobs.JobStatusChanged"/> to convert from.</param>
    /// <returns>Converted <see cref="JobStatusChanged"/>.</returns>
    public static JobStatusChanged ToContract(this Concepts.Jobs.JobStatusChanged statusChanged) =>
        new()
        {
            Status = (JobStatus)(int)statusChanged.Status,
            Occurred = statusChanged.Occurred!,
            ExceptionMessages = statusChanged.ExceptionMessages.ToList(),
            ExceptionStackTrace = statusChanged.ExceptionStackTrace
        };

    /// <summary>
    /// Convert from collection of <see cref="Concepts.Jobs.JobStatusChanged"/> to collection of <see cref="JobStatusChanged"/>.
    /// </summary>
    /// <param name="statusChanged">Collection of <see cref="Concepts.Jobs.JobStatusChanged"/> to convert from.</param>
    /// <returns>Converted collection of <see cref="JobStatusChanged"/>.</returns>
    public static IList<JobStatusChanged> ToContract(this IEnumerable<Concepts.Jobs.JobStatusChanged> statusChanged) => statusChanged.Select(_ => _.ToContract()).ToList();
}
