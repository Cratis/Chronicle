// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Jobs;

/// <summary>
/// Extension methods for <see cref="IJobTypes"/>.
/// </summary>
public static class JobTypesExtensions
{
    /// <summary>
    /// Gets the <see cref="JobType"/> associated with the CLR <see cref="Type"/> or throws an exception.
    /// </summary>
    /// <param name="jobTypes">The job types.</param>
    /// <param name="type">The <see cref="Type"/>.</param>
    /// <returns><see cref="JobType"/>.</returns>
    public static JobType GetForOrThrow(this IJobTypes jobTypes, Type type) => jobTypes.GetFor(type).Match(
        jobType => jobType,
        error => error switch
        {
            IJobTypes.GetForError.NoAssociatedJobType => throw new JobTypeNotAssociatedWithType(type),
            _ => throw new ArgumentOutOfRangeException(nameof(error), error, null)
        });

    /// <summary>
    /// Gets the job <see cref="Type"/> associated with the <see cref="JobType"/> or throws an exception.
    /// </summary>
    /// <param name="jobTypes">The job types.</param>
    /// <param name="jobType">The <see cref="Type"/>.</param>
    /// <returns><see cref="Type"/>.</returns>
    public static Type GetClrTypeForOrThrow(this IJobTypes jobTypes, JobType jobType) => jobTypes.GetClrTypeFor(jobType).Match(
        type => type,
        error => error switch
        {
            IJobTypes.GetClrTypeForError.JobTypeIsNotSet => throw new UnknownClrTypeForJobType(jobType),
            IJobTypes.GetClrTypeForError.CouldNotFindType => throw new UnknownClrTypeForJobType(jobType),
            _ => throw new ArgumentOutOfRangeException(nameof(error), error, null)
        });

    /// <summary>
    /// Gets the job request <see cref="Type"/> associated with the <see cref="JobType"/> or throws an exception.
    /// </summary>
    /// <param name="jobTypes">The job types.</param>
    /// <param name="jobType">The <see cref="Type"/>.</param>
    /// <returns><see cref="Type"/>.</returns>
    public static Type GetRequestClrTypeForOrThrow(this IJobTypes jobTypes, JobType jobType) => jobTypes.GetRequestClrTypeFor(jobType).Match(
        type => type,
        error => throw new UnknownClrTypeForJobType(jobType));
}
