// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Workers;

#pragma warning disable MA0048 // File name must match type name

internal static partial class CpuBoundWorkerLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Task has been cancelled")]
    internal static partial void TaskHasBeenCancelled(this ILogger<ICpuBoundWorker> logger);

    [LoggerMessage(LogLevel.Debug, "Beginning work for task")]
    internal static partial void BeginningWorkForTask(this ILogger<ICpuBoundWorker> logger);

    [LoggerMessage(LogLevel.Trace, "Task has completed")]
    internal static partial void TaskHasCompleted(this ILogger<ICpuBoundWorker> logger);

    [LoggerMessage(LogLevel.Warning, "Task has failed")]
    internal static partial void TaskHasFailed(this ILogger<ICpuBoundWorker> logger, Exception exception);

    [LoggerMessage(LogLevel.Debug, "Task has already been initialized")]
    internal static partial void TaskHasAlreadyBeenInitialized(this ILogger<ICpuBoundWorker> logger);

    [LoggerMessage(LogLevel.Debug, "Starting task")]
    internal static partial void StartingTask(this ILogger<ICpuBoundWorker> logger);

    [LoggerMessage(LogLevel.Debug, "Task is no longer in the scheduler")]
    internal static partial void TaskIsNoLongerInTheScheduler(this ILogger<ICpuBoundWorker> logger);
}
