// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Extension methods for converting between <see cref="Contracts.Jobs.Job"/> and <see cref="Job"/>.
/// </summary>
public static class JobsConverters
{
    /// <summary>
    /// Convert from <see cref="Contracts.Jobs.Job"/> to <see cref="Job"/>.
    /// </summary>
    /// <param name="job"><see cref="Contracts.Jobs.Job"/> to convert from.</param>
    /// <param name="eventStore">EventStore the job belongs to.</param>
    /// <returns>Converted <see cref="Job"/>.</returns>
    public static Job ToClient(this Contracts.Jobs.Job job, IEventStore eventStore) =>
        new(eventStore)
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
    /// <param name="jobs">Collection of <see cref="Contracts.Jobs.Job"/> to convert from.</param>
    /// <param name="eventStore">EventStore the job belongs to.</param>
    /// <returns>Converted collection of <see cref="Job"/>.</returns>
    public static IEnumerable<Job> ToClient(this IEnumerable<Contracts.Jobs.Job> jobs, IEventStore eventStore) => jobs.Select(_ => _.ToClient(eventStore)).ToArray();
}
