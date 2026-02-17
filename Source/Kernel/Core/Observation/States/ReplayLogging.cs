// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.States;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class ReplayLogMessages
{
    [LoggerMessage(LogLevel.Trace, "Entering replay state")]
    internal static partial void Entering(this ILogger<Replay> logger);

    [LoggerMessage(LogLevel.Trace, "Existing replay job running, will let it finish")]
    internal static partial void FinishingExistingReplayJob(this ILogger<Replay> logger);

    [LoggerMessage(LogLevel.Trace, "Existing replay job found - resuming")]
    internal static partial void ResumingReplayJob(this ILogger<Replay> logger);

    [LoggerMessage(LogLevel.Trace, "Start new replay job")]
    internal static partial void StartReplayJob(this ILogger<Replay> logger);
}

internal static class ReplayScopes
{
    internal static IDisposable? BeginReplayScope(this ILogger<Replay> logger, ObserverId observerId, ObserverKey observerKey) =>
        logger.BeginScope(new
        {
            ObserverId = observerId,
            observerKey.EventStore,
            observerKey.Namespace,
            observerKey.EventSequenceId
        });
}
