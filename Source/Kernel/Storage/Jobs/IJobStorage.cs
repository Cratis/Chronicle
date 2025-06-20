// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Monads;
using OneOf.Types;

namespace Cratis.Chronicle.Storage.Jobs;

/// <summary>
/// Defines a system that can store job state.
/// </summary>
public interface IJobStorage
{
    /// <summary>
    /// Get a specific job.
    /// </summary>
    /// <param name="jobId">The unique <see cref="JobId"/> for the job to get.</param>
    /// <returns>The job instance.</returns>
    Task<Catch<JobState, JobError>> GetJob(JobId jobId);

    /// <summary>
    /// Get all jobs of a given type with a given status.
    /// </summary>
    /// <param name="statuses">Optional params of <see cref="JobStatus"/> to filter on.</param>
    /// <returns>A collection of job state objects.</returns>
    /// <remarks>
    /// If no job statuses are specified, all jobs will be returned.
    /// </remarks>
    Task<Catch<IImmutableList<JobState>>> GetJobs(params JobStatus[] statuses);

    /// <summary>
    /// Observe jobs of a given type with a given status.
    /// </summary>
    /// <param name="statuses">Optional params of <see cref="JobStatus"/> to filter on.</param>
    /// <returns>An observable of collection of job state objects.</returns>
    /// <remarks>
    /// If no job statuses are specified, all jobs will be observed.
    /// </remarks>
    Catch<ISubject<IEnumerable<JobState>>> ObserveJobs(params JobStatus[] statuses);

    /// <summary>
    /// Remove a job.
    /// </summary>
    /// <param name="jobId">The <see cref="JobId"/> of the job to remove.</param>
    /// <returns>Awaitable task.</returns>
    Task<Catch> Remove(JobId jobId);

    /// <summary>
    /// Read the state of a job.
    /// </summary>
    /// <param name="jobId">The <see cref="JobId"/> of the job to read.</param>
    /// <returns>Job state instance or null if it doesn't exist.</returns>
    /// <typeparam name="TJobState">Type of <see cref="JobState"/> to return.</typeparam>
    Task<Catch<TJobState, JobError>> Read<TJobState>(JobId jobId);

    /// <summary>
    /// Save the state of a job.
    /// </summary>
    /// <param name="jobId">The <see cref="JobId"/> of the job to save.</param>
    /// <param name="state"><see cref="JobState"/> to save.</param>
    /// <typeparam name="TJobState">Type of <see cref="JobState"/> to save.</typeparam>
    /// <returns>Awaitable task.</returns>
    Task<Catch<None, JobError>> Save<TJobState>(JobId jobId, TJobState state);

    /// <summary>
    /// Get all jobs of a given type with a given status.
    /// </summary>
    /// <typeparam name="TJobType">Type of job to get for.</typeparam>
    /// <param name="statuses">Optional params of <see cref="JobStatus"/> to filter on.</param>
    /// <returns>A collection of job state objects.</returns>
    /// <typeparam name="TJobState">Type of <see cref="JobState"/> to return.</typeparam>
    /// <remarks>
    /// If no job statuses are specified, all jobs of the given type will be returned.
    /// </remarks>
    Task<Catch<IImmutableList<TJobState>, JobError>> GetJobs<TJobType, TJobState>(params JobStatus[] statuses);
}
