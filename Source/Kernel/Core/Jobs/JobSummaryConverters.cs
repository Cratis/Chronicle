// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Converts stored job state into the job summary read model.
/// </summary>
/// <remarks>
/// These live beside the read model rather than on it because a static method on a <c>[ReadModel]</c> whose return
/// shape is a supported query shape becomes a generated query proxy and an HTTP endpoint - accessibility is not
/// what the proxy generator looks at. A conversion helper is not an operation anybody should be able to call.
/// </remarks>
internal static class JobSummaryConverters
{
    /// <summary>
    /// Converts stored job states into job summaries.
    /// </summary>
    /// <param name="jobs">The stored job states.</param>
    /// <returns>The job summaries.</returns>
    internal static IEnumerable<JobSummary> ToJobs(IEnumerable<JobState> jobs) => jobs.Select(ToJob);

    /// <summary>
    /// Converts a stored job state into a job summary.
    /// </summary>
    /// <param name="job">The stored job state.</param>
    /// <returns>The job summary.</returns>
    internal static JobSummary ToJob(JobState job) =>
        new(
            (Guid)job.Id,
            job.Details,
            job.Type,
            job.Status,
            job.Created,
            job.StatusChanges.Select(ToStatusChanged),
            ToProgress(job.Progress));

    /// <summary>
    /// Converts a stored status change into its contract representation.
    /// </summary>
    /// <param name="sc">The stored status change.</param>
    /// <returns>The status change.</returns>
    internal static JobStatusChanged ToStatusChanged(Concepts.Jobs.JobStatusChanged sc) =>
        new()
        {
            Status = (JobStatus)(int)sc.Status,
            Occurred = sc.Occurred,
            ExceptionMessages = sc.ExceptionMessages.ToList(),
            ExceptionStackTrace = sc.ExceptionStackTrace
        };

    /// <summary>
    /// Converts stored progress into its contract representation.
    /// </summary>
    /// <param name="p">The stored progress.</param>
    /// <returns>The progress.</returns>
    internal static JobProgress ToProgress(Concepts.Jobs.JobProgress p) =>
        new()
        {
            TotalSteps = p.TotalSteps,
            SuccessfulSteps = p.SuccessfulSteps,
            FailedSteps = p.FailedSteps,
            StoppedSteps = p.StoppedSteps,
            IsCompleted = p.IsCompleted,
            IsStopped = p.IsStopped,
            Message = p.Message
        };
}
