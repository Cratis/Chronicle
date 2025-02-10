// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Services.Jobs;

/// <summary>
/// Extension methods for converting between <see cref="JobState"/> and <see cref="Job"/>.
/// </summary>
public static class JobsConverters
{
    /// <summary>
    /// Convert from <see cref="JobState"/> to <see cref="Job"/>.
    /// </summary>
    /// <param name="job"><see cref="JobState"/> to convert from.</param>
    /// <returns>Converted <see cref="Job"/>.</returns>
    public static Job ToContract(this JobState job) =>
        new()
        {
            Id = job.Id,
            Details = job.Details,
            Type = job.Type,
            Status = (JobStatus)(int)job.Status,
            Created = job.Created!,
            StatusChanges = job.StatusChanges.ToContract(),
            Progress = job.Progress.ToContract()
        };

    /// <summary>
    /// Convert from <see cref="IEnumerable{JobState}"/> to <see cref="IEnumerable{Job}"/>.
    /// </summary>
    /// <param name="jobs">Collection of <see cref="JobState"/> to convert from.</param>
    /// <returns>Converted collection of <see cref="Job"/>.</returns>
    public static IEnumerable<Job> ToContract(this IEnumerable<JobState> jobs) => jobs.Select(_ => _.ToContract()).ToArray();
}
