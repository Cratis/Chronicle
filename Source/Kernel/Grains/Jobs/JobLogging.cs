// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Jobs;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Jobs;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class JobLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Starting job")]
    internal static partial void Starting(this ILogger<IJob> logger);

    [LoggerMessage(1, LogLevel.Information, "Resuming job")]
    internal static partial void Resuming(this ILogger<IJob> logger);

    [LoggerMessage(2, LogLevel.Information, "Pausing job")]
    internal static partial void Pausing(this ILogger<IJob> logger);

    [LoggerMessage(3, LogLevel.Information, "Stopping job")]
    internal static partial void Stopping(this ILogger<IJob> logger);

    [LoggerMessage(4, LogLevel.Trace, "Step {JobStepId} successfully completed")]
    internal static partial void StepSuccessfullyCompleted(this ILogger<IJob> logger, JobStepId jobStepId);

    [LoggerMessage(5, LogLevel.Trace, "Step {JobStepId} failed")]
    internal static partial void StepFailed(this ILogger<IJob> logger, JobStepId jobStepId);

    [LoggerMessage(6, LogLevel.Trace, "Preparing job steps for running")]
    internal static partial void PrepareJobStepsForRunning(this ILogger<IJob> logger);

    [LoggerMessage(7, LogLevel.Error, "Job failed")]
    internal static partial void Failed(this ILogger<IJob> logger, Exception exception);
}

internal static class JobScopes
{
    internal static IDisposable? BeginJobScope(this ILogger<IJob> logger, JobId jobId, JobKey key) =>
        logger.BeginScope(new Dictionary<string, object>
        {
            ["JobId"] = jobId,
            ["MicroserviceId"] = key.MicroserviceId,
            ["TenantId"] = key.TenantId
        });
}
