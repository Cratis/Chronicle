// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Workers;

internal static partial class WorkerLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Worker {Worker} started")]
    internal static partial void WorkerStarted(this ILogger logger, string worker);

    [LoggerMessage(1, LogLevel.Information, "Worker {Worker} completed")]
    internal static partial void WorkerCompleted(this ILogger logger, string worker);

    [LoggerMessage(2, LogLevel.Error, "Worker {Worker} failed")]
    internal static partial void WorkerFailed(this ILogger logger, string worker, Exception exception);
}
