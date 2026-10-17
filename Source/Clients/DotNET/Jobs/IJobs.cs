// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Defines the system for working with jobs.
/// </summary>
public interface IJobs
{
    /// <summary>
    /// Stops the <see cref="Job"/> with the specified <paramref name="jobId"/>.
    /// </summary>
    /// <param name="jobId">The job id.</param>
    /// <returns>The <see cref="JobCommandResult"/> telling whether the job took the stop, and why not when it did not.</returns>
    Task<JobCommandResult> Stop(JobId jobId);

    /// <summary>
    /// Resumes the <see cref="Job"/> with the specified <paramref name="jobId"/>.
    /// </summary>
    /// <param name="jobId">The job id.</param>
    /// <returns>The <see cref="JobCommandResult"/> telling whether the job was taken forward, and why not when it was not.</returns>
    Task<JobCommandResult> Resume(JobId jobId);

    /// <summary>
    /// Deletes the <see cref="Job"/> with the specified <paramref name="jobId"/>.
    /// </summary>
    /// <param name="jobId">The job id.</param>
    /// <returns>Awaitable task.</returns>
    Task Delete(JobId jobId);

    /// <summary>
    /// Gets the <see cref="Job"/> with the specified <paramref name="jobId"/>.
    /// </summary>
    /// <param name="jobId">The job id.</param>
    /// <returns>The job.</returns>
    Task<Job?> GetJob(JobId jobId);

    /// <summary>
    /// Gets all the <see cref="Job"/>s.
    /// </summary>
    /// <returns>The jobs.</returns>
    Task<IEnumerable<Job>> GetJobs();
}
