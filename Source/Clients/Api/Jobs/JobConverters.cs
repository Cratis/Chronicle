// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Jobs;

/// <summary>
/// Converters between <see cref="Job"/> to a <see cref="Job"/> for the API.
/// </summary>
internal static class JobConverters
{
    /// <summary>
    /// Converts a <see cref="Contracts.Jobs.Job"/> to a <see cref="Job"/> for the API.
    /// </summary>
    /// <param name="job">The job to convert.</param>
    /// <returns>The converted job.</returns>
    public static Job ToApi(this Contracts.Jobs.Job job) => new(
        job.Id,
        job.Details,
        job.Type,
        (JobStatus)(int)job.Status,
        job.Created,
        job.StatusChanges.ToApi(),
        job.Progress.ToApi());

    /// <summary>
    /// Converts a collection of <see cref="Contracts.Jobs.Job"/> to a collection of <see cref="Job"/> for the API.
    /// </summary>
    /// <param name="jobs">The collection of jobs to convert.</param>
    /// <returns>The converted collection of jobs.</returns>
    public static IEnumerable<Job> ToApi(this IEnumerable<Contracts.Jobs.Job> jobs) =>
        jobs.Select(ToApi).ToArray();
}
