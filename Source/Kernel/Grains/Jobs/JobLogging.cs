// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Jobs;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class JobLogMessages
{
    [LoggerMessage(LogLevel.Warning, "An error occurred while writing state")]
    internal static partial void FailedWritingState(this ILogger<IJob> logger, Exception ex);

    [LoggerMessage(LogLevel.Information, "Starting job")]
    internal static partial void Starting(this ILogger<IJob> logger);

    [LoggerMessage(LogLevel.Debug, "Job changing status to {Status}")]
    internal static partial void ChangingStatus(this ILogger<IJob> logger, JobStatus status);

    [LoggerMessage(LogLevel.Debug, "There are no prepared job steps. Completing job")]
    internal static partial void NoJobStepsToStart(this ILogger<IJob> logger);

    [LoggerMessage(LogLevel.Debug, "Found {JobStepsCount} job steps to prepare and start")]
    internal static partial void PreparingJobSteps(this ILogger<IJob> logger, int jobStepsCount);

    [LoggerMessage(LogLevel.Debug, "An error occurred while preparing and starting job step {JobStepId}")]
    internal static partial void ErrorPreparingJobStep(this ILogger<IJob> logger, Exception ex, JobStepId jobStepId);

    [LoggerMessage(LogLevel.Warning, "An error occurred while preparing and starting job steps")]
    internal static partial void ErrorPreparingJobSteps(this ILogger<IJob> logger, Exception ex);

    [LoggerMessage(LogLevel.Information, "Resuming job")]
    internal static partial void Resuming(this ILogger<IJob> logger);

    [LoggerMessage(LogLevel.Information, "Pausing job")]
    internal static partial void Pausing(this ILogger<IJob> logger);

    [LoggerMessage(LogLevel.Information, "Stopping job")]
    internal static partial void Stopping(this ILogger<IJob> logger);

    [LoggerMessage(LogLevel.Trace, "Step {JobStepId} successfully completed")]
    internal static partial void StepSuccessfullyCompleted(this ILogger<IJob> logger, JobStepId jobStepId);

    [LoggerMessage(LogLevel.Trace, "Step {JobStepId} failed")]
    internal static partial void StepFailed(this ILogger<IJob> logger, JobStepId jobStepId);

    [LoggerMessage(LogLevel.Trace, "Preparing job steps for running")]
    internal static partial void PrepareJobStepsForRunning(this ILogger<IJob> logger);

    [LoggerMessage(LogLevel.Warning, "Job failed")]
    internal static partial void Failed(this ILogger<IJob> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Job failed to get job steps")]
    internal static partial void FailedToGetJobSteps(this ILogger<IJob> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Job failed to persist new total steps count")]
    internal static partial void FailedToSetTotalSteps(this ILogger<IJob> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Job failed to persist new job status {Status}")]
    internal static partial void FailedWritingStatusChange(this ILogger<IJob> logger, JobStatus status);

    [LoggerMessage(LogLevel.Warning, "Job failed to persist new updated successful steps {SuccessfulStepsCount}")]
    internal static partial void FailedUpdatingSuccessfulSteps(this ILogger<IJob> logger, Exception ex, int successfulStepsCount);

    [LoggerMessage(LogLevel.Warning, "Job failed to persist new updated failed steps {FailedStepsCount}")]
    internal static partial void FailedUpdatingFailedSteps(this ILogger<IJob> logger, Exception ex, int failedStepsCount);

    [LoggerMessage(LogLevel.Warning, "Job failed remove state for all job steps and the job itself")]
    internal static partial void FailedToRemoveForJob(this ILogger<IJob> logger, Exception error);

    [LoggerMessage(LogLevel.Warning, "Job failed on completed. Error: {JobError}")]
    internal static partial void FailedOnCompleted(this ILogger<IJob> logger, JobError jobError);

    [LoggerMessage(LogLevel.Warning, "Job failed on completed")]
    internal static partial void FailedOnCompleted(this ILogger<IJob> logger, Exception error);

    [LoggerMessage(LogLevel.Warning, "Job {JobId} {JobType} failed unexpectedly on OnCompleted")]
    internal static partial void FailedOnCompleted(this ILogger<IJob> logger, Exception ex, JobId jobId, JobType jobType);

    [LoggerMessage(LogLevel.Warning, "Job failed on completed while there are no job steps to run, but Job will still clear state. Error: {JobError}")]
    internal static partial void FailedOnCompletedWhileNoJobSteps(this ILogger<IJob> logger, JobError jobError);

    [LoggerMessage(LogLevel.Debug, "Job is not in a running state. Status: {JobStatus}")]
    internal static partial void JobIsNotRunning(this ILogger<IJob> logger, JobStatus jobStatus);

    [LoggerMessage(LogLevel.Warning, "Job failed handling completed job step {JobStepId}. Error: {JobError}")]
    internal static partial void FailedHandlingCompletedJobStep(this ILogger<IJob> logger, JobStepId jobStepId, JobError jobError);

    [LoggerMessage(LogLevel.Warning, "Job failed persisting state after handling job step completion for job step {JobStepId}")]
    internal static partial void FailedUpdatingStateAfterHandlingJobStepCompletion(this ILogger<IJob> logger, Exception ex, JobStepId jobStepId);

    [LoggerMessage(LogLevel.Warning, "Job failed preparing job step {JobStepId}. Error: {Error}")]
    internal static partial void FailedPreparingJobStep(this ILogger<IJob> logger, JobStepId jobStepId, JobStepPrepareStartError error);

    [LoggerMessage(LogLevel.Warning, "Job failed starting job step {JobStepId}. Error: {Error}")]
    internal static partial void FailedStartingJobStep(this ILogger<IJob> logger, JobStepId jobStepId, JobStepPrepareStartError error);

    [LoggerMessage(LogLevel.Debug, "Not all steps was completed successfully")]
    internal static partial void AllStepsNotCompletedSuccessfully(this ILogger<IJob> logger);
}

internal static class JobScopes
{
    internal static IDisposable? BeginJobScope(this ILogger<IJob> logger, JobId jobId, JobKey key) =>
        logger.BeginScope(new
        {
            JobId = jobId,
            key.EventStore,
            key.Namespace
        });
}
