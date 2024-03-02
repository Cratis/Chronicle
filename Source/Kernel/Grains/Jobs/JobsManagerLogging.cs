// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Jobs;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Jobs;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name

internal static partial class JobsManagerLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Rehydrating jobs system")]
    internal static partial void Rehydrating(this ILogger<JobsManager> logger);

    [LoggerMessage(1, LogLevel.Information, "Starting job {JobId}")]
    internal static partial void StartingJob(this ILogger<JobsManager> logger, JobId jobId);

    [LoggerMessage(2, LogLevel.Information, "Resuming job {JobId}")]
    internal static partial void ResumingJob(this ILogger<JobsManager> logger, JobId jobId);

    [LoggerMessage(3, LogLevel.Information, "Stopping job {JobId}")]
    internal static partial void StoppingJob(this ILogger<JobsManager> logger, JobId jobId);

    [LoggerMessage(4, LogLevel.Information, "Deleting job {JobId}")]
    internal static partial void DeletingJob(this ILogger<JobsManager> logger, JobId jobId);

    [LoggerMessage(5, LogLevel.Information, "Job {JobId} completed with status {Status}")]
    internal static partial void JobCompleted(this ILogger<JobsManager> logger, JobId jobId, JobStatus status);
}

internal static class JobsManagerScopes
{
    internal static IDisposable? BeginJobsManagerScope(this ILogger<JobsManager> logger, JobsManagerKey key) =>
        logger.BeginScope(new Dictionary<string, object>
        {
            ["MicroserviceId"] = key.MicroserviceId,
            ["TenantId"] = key.TenantId
        });
}
