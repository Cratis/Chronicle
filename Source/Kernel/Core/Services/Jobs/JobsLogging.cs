// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.Jobs;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class JobsLogMessages
{
    /// <summary>
    /// Logs a failure to observe jobs from storage.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger{TCategoryName}"/> to log to.</param>
    /// <param name="exception">The <see cref="Exception"/> that caused the failure.</param>
    /// <param name="eventStore">The event store the jobs were observed for.</param>
    /// <param name="namespace">The namespace the jobs were observed for.</param>
    [LoggerMessage(LogLevel.Error, "Failed to observe jobs for event store {EventStore} and namespace {Namespace}")]
    internal static partial void FailedToObserveJobs(this ILogger<Jobs> logger, Exception exception, string eventStore, string @namespace);

    /// <summary>
    /// Logs a failure to get job steps from storage.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger{TCategoryName}"/> to log to.</param>
    /// <param name="exception">The <see cref="Exception"/> that caused the failure.</param>
    /// <param name="jobId">The identifier of the job the steps were requested for.</param>
    /// <param name="eventStore">The event store the job steps were requested for.</param>
    /// <param name="namespace">The namespace the job steps were requested for.</param>
    [LoggerMessage(LogLevel.Error, "Failed to get job steps for job {JobId} in event store {EventStore} and namespace {Namespace}")]
    internal static partial void FailedToGetJobSteps(this ILogger<Jobs> logger, Exception exception, Guid jobId, string eventStore, string @namespace);
}
