// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Jobs;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class JobStepLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Starting job step")]
    internal static partial void Starting(this ILogger<IJobStep> logger);

    [LoggerMessage(LogLevel.Warning, "Could not start job step because preparation failed with '{PrepareError}'")]
    internal static partial void StartPrepareFailed(this ILogger<IJobStep> logger, JobStepError prepareError);

    [LoggerMessage(LogLevel.Debug, "Resuming job step")]
    internal static partial void Resuming(this ILogger<IJobStep> logger);

    [LoggerMessage(LogLevel.Debug, "Pausing job step")]
    internal static partial void Pausing(this ILogger<IJobStep> logger);

    [LoggerMessage(LogLevel.Debug, "Stopping job step")]
    internal static partial void Stopping(this ILogger<IJobStep> logger);

    [LoggerMessage(LogLevel.Warning, "Job step handling unexpected error from performing job step")]
    internal static partial void HandleUnexpectedPerformJobStepFailure(this ILogger<IJobStep> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Job step failed with an unexpected error")]
    internal static partial void FailedUnexpectedly(this ILogger<IJobStep> logger, Exception exception);

    [LoggerMessage(LogLevel.Debug, "Job step reporting failure")]
    internal static partial void ReportFailure(this ILogger<IJobStep> logger);

    [LoggerMessage(LogLevel.Warning, "Job step failed to report failure. This might cause unexpected behaviour")]
    internal static partial void FailedToReportFailure(this ILogger<IJobStep> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Job step failed to write persisted state")]
    internal static partial void FailedToWriteState(this ILogger<IJobStep> logger, Exception exception);
}

internal static class JobStepScopes
{
    internal static IDisposable? BeginJobStepScope(this ILogger<IJobStep> logger, JobStepState state) =>
        logger.BeginScope(new Dictionary<string, object>
        {
            ["JobId"] = state.Id.JobId,
            ["JobStepId"] = state.Id.JobStepId,
            ["JobStepName"] = state.Name,
            ["JobStepStatus"] = state.Status
        });
}
