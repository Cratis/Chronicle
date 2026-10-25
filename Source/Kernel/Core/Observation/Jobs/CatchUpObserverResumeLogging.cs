// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Jobs;

#pragma warning disable SA1600 // Elements should be documented

internal static partial class CatchUpObserverResumeLogging
{
    [LoggerMessage(LogLevel.Information, "Catch-up job has no saved step plan, so the partitions it owns are unknown; live delivery stays held back until it concludes")]
    internal static partial void NoStepPlanToRegisterPartitionsFrom(this ILogger<CatchUpObserver> logger);

    [LoggerMessage(LogLevel.Information, "The observer did not accept this catch-up job as its owner, so the partitions it owns are not registered")]
    internal static partial void ObserverRefusedCatchUpOwnership(this ILogger<CatchUpObserver> logger);

    [LoggerMessage(LogLevel.Warning, "Could not register the partitions a catch-up job owns; live delivery stays held back until it concludes")]
    internal static partial void FailedToRegisterPartitionOwnership(this ILogger<CatchUpObserver> logger, Exception exception);
}
