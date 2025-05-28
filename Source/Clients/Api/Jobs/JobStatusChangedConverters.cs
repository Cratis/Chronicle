// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Jobs;

/// <summary>
/// Converters between <see cref="JobStatusChanged"/> to a <see cref="JobStatusChanged"/> for the API.
/// </summary>
internal static class JobStatusChangedConverters
{
    /// <summary>
    /// Converts a <see cref="Contracts.Jobs.JobStatusChanged"/> to a <see cref="JobStatusChanged"/> for the API.
    /// </summary>
    /// <param name="jobStatusChanged">The job status changed to convert.</param>
    /// <returns>The converted job status changed.</returns>
    public static JobStatusChanged ToApi(this Contracts.Jobs.JobStatusChanged jobStatusChanged) => new(
        (JobStatus)(int)jobStatusChanged.Status,
        jobStatusChanged.Occurred,
        jobStatusChanged.ExceptionMessages,
        jobStatusChanged.ExceptionStackTrace);

    /// <summary>
    /// Converts a collection of <see cref="JobStatusChanged"/> to a collection <see cref="Contracts.Jobs.JobStatusChanged"/> for the API.
    /// </summary>
    /// <param name="jobStatusChanged">The collection of job status changed to convert.</param>
    /// <returns>The converted collection of job status changed.</returns>
    public static IEnumerable<JobStatusChanged> ToApi(this IEnumerable<Contracts.Jobs.JobStatusChanged> jobStatusChanged) =>
        jobStatusChanged.Select(x => x.ToApi()).ToArray();
}
