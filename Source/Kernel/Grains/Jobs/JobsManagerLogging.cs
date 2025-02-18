// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Jobs;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class JobsManagerLogMessages
{
    [LoggerMessage(LogLevel.Information, "Rehydrating jobs system")]
    internal static partial void Rehydrating(this ILogger<JobsManager> logger);

    [LoggerMessage(LogLevel.Debug, "Starting job {JobId}")]
    internal static partial void StartingJob(this ILogger<JobsManager> logger, JobId jobId);

    [LoggerMessage(LogLevel.Debug, "Resuming job {JobId}")]
    internal static partial void ResumingJob(this ILogger<JobsManager> logger, JobId jobId);

    [LoggerMessage(LogLevel.Warning, "Error while resuming job {JobId}")]
    internal static partial void ErrorResumingJob(this ILogger<JobsManager> logger, Exception ex, JobId jobId);

    [LoggerMessage(LogLevel.Debug, "Stopping job {JobId}")]
    internal static partial void StoppingJob(this ILogger<JobsManager> logger, JobId jobId);

    [LoggerMessage(LogLevel.Debug, "Deleting job {JobId}")]
    internal static partial void DeletingJob(this ILogger<JobsManager> logger, JobId jobId);

    [LoggerMessage(LogLevel.Debug, "Job {JobId} completed with status {Status}")]
    internal static partial void JobCompleted(this ILogger<JobsManager> logger, JobId jobId, JobStatus status);

    [LoggerMessage(LogLevel.Warning, "An unknown error occurred in Job {JobId}")]
    internal static partial void UnknownError(this ILogger<JobsManager> logger, Exception exception, JobId jobId);

    [LoggerMessage(LogLevel.Warning, "An unknown error occurred")]
    internal static partial void UnknownError(this ILogger<JobsManager> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Job {JobId} could not be found")]
    internal static partial void JobCouldNotBeFound(this ILogger<JobsManager> logger, JobId jobId);

    [LoggerMessage(LogLevel.Warning, "Job {JobId} an error occurred while performing action. {JobError}")]
    internal static partial void JobErrorOccurred(this ILogger<JobsManager> logger, JobId jobId, JobError jobError);

    [LoggerMessage(LogLevel.Warning, "Job {JobId} an error occurred while resuming job steps. {JobSteps}")]
    internal static partial void FailedToResumeJobSteps(this ILogger<JobsManager> logger, JobId jobId, IEnumerable<JobStepId> jobSteps);

    [LoggerMessage(LogLevel.Warning, "Job {JobId} cannot be resumed because it is running")]
    internal static partial void CannotResumeJobBecauseAlreadyRunning(this ILogger<JobsManager> logger, JobId jobId);

    [LoggerMessage(LogLevel.Warning, "Job {JobId} cannot be resumed")]
    internal static partial void CannotResumeJob(this ILogger<JobsManager> logger, JobId jobId);

    [LoggerMessage(LogLevel.Warning, "Job {JobId} encountered error : {Error}")]
    internal static partial void JobErrorOccurred(this ILogger<JobsManager> logger, JobId jobId, Storage.Jobs.JobError error);

    [LoggerMessage(LogLevel.Warning, "Unable to get jobs of type {JobType}. Encountered error : {Error}")]
    internal static partial void UnableToGetJobs(this ILogger<JobsManager> logger, Type jobType, Storage.Jobs.JobError error);

    [LoggerMessage(LogLevel.Warning, "Unable to get all jobs. Encountered error")]
    internal static partial void UnableToGetAllJobs(this ILogger<JobsManager> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Failed to stop Job {JobId}")]
    internal static partial void FailedToStopJob(this ILogger<JobsManager> logger, JobId jobId);
}

internal static class JobsManagerScopes
{
    internal static IDisposable? BeginJobsManagerScope(this ILogger<JobsManager> logger, JobsManagerKey key) =>
        logger.BeginScope(new
        {
            key.EventStore,
            key.Namespace
        });
}
