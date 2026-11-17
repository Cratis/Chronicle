// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Log messages for <see cref="ObserverJobStarter{TJob, TRequest}"/>.
/// </summary>
internal static partial class ObserverJobStarterLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Found already running job {JobId}")]
    internal static partial void FoundRunningJob(this ILogger logger, JobId jobId);

    [LoggerMessage(LogLevel.Debug, "Found stopped job {JobId}. Resuming it")]
    internal static partial void FoundStoppedJob(this ILogger logger, JobId jobId);

    [LoggerMessage(LogLevel.Debug, "Need to start new job")]
    internal static partial void NeedToStartJob(this ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Could not start job: {Error}")]
    internal static partial void CouldNotStartJob(this ILogger logger, StartJobError error);

    [LoggerMessage(LogLevel.Warning, "Could not resume stopped job {JobId} - nothing will drive it forward")]
    internal static partial void CouldNotResumeJob(this ILogger logger, JobId jobId);

    [LoggerMessage(LogLevel.Warning, "Deleting job {JobId} that stayed in preparation without a step - nothing will ever drive it forward")]
    internal static partial void DeletingAbandonedPreparingJob(this ILogger logger, JobId jobId);

    [LoggerMessage(LogLevel.Warning, "Dropping stopped job {JobId} that was never prepared and can never resume")]
    internal static partial void DroppingJobThatCanNeverResume(this ILogger logger, JobId jobId);

    [LoggerMessage(LogLevel.Warning, "Could not drop stopped job {JobId} that can never resume")]
    internal static partial void CouldNotDropJob(this ILogger logger, JobId jobId, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Stopped job {JobId} never prepared a step and can not be resumed - removing it so catch-up can start a new job")]
    internal static partial void RemovingUnresumableJob(this ILogger logger, JobId jobId);
}
