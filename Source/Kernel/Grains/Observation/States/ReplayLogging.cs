// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Observation.States;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name

internal static partial class ReplayLogMessages
{
    [LoggerMessage(0, LogLevel.Trace, "Entering replay state")]
    internal static partial void Entering(this ILogger<Replay> logger);

    [LoggerMessage(1, LogLevel.Trace, "Existing replay job running, will let it finish")]
    internal static partial void FinishingExistingReplayJob(this ILogger<Replay> logger);

    [LoggerMessage(2, LogLevel.Trace, "Existing replay job found - resuming")]
    internal static partial void ResumingReplayJob(this ILogger<Replay> logger);

    [LoggerMessage(3, LogLevel.Trace, "Start new replay job")]
    internal static partial void StartReplayJob(this ILogger<Replay> logger);
}

internal static class ReplayScopes
{
    internal static IDisposable? BeginReplayScope(this ILogger<Replay> logger, ObserverId observerId, ObserverKey observerKey) =>
        logger.BeginScope(new Dictionary<string, object>
        {
            ["ObserverId"] = observerId,
            ["MicroserviceId"] = observerKey.MicroserviceId,
            ["TenantId"] = observerKey.TenantId,
            ["EventSequenceId"] = observerKey.EventSequenceId
        });
}
