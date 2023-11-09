// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Workers;

#pragma warning disable MA0048 // File name must match type name

internal static partial class CpuBoundWorkerLogMessages
{
    [LoggerMessage(0, LogLevel.Trace, "Task has been cancelled")]
    internal static partial void TaskHasBeenCancelled(this ILogger<ICpuBoundWorker> logger);

    [LoggerMessage(1, LogLevel.Trace, "Beginning work for task")]
    internal static partial void BeginningWorkForTask(this ILogger<ICpuBoundWorker> logger);

    [LoggerMessage(2, LogLevel.Trace, "Task has completed")]
    internal static partial void TaskHasCompleted(this ILogger<ICpuBoundWorker> logger);

    [LoggerMessage(3, LogLevel.Trace, "Task has failed")]
    internal static partial void TaskHasFailed(this ILogger<ICpuBoundWorker> logger, Exception exception);

    [LoggerMessage(4, LogLevel.Trace, "Task has already been initialized")]
    internal static partial void TaskHasAlreadyBeenInitialized(this ILogger<ICpuBoundWorker> logger);

    [LoggerMessage(5, LogLevel.Trace, "Starting task")]
    internal static partial void StartingTask(this ILogger<ICpuBoundWorker> logger);

    [LoggerMessage(6, LogLevel.Trace, "Task is no longer in the scheduler")]
    internal static partial void TaskIsNoLongerInTheScheduler(this ILogger<ICpuBoundWorker> logger);
}
