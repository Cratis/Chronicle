// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Holds log messages for <see cref="DormantTargetSweep"/> and <see cref="DormantSiloDepartures"/>.
/// </summary>
internal static partial class DormantTargetSweepLogging
{
    [LoggerMessage(LogLevel.Warning, "Could not tell dormant observer '{ObserverId}' that its client or silo is gone. The safety net of its queue finds it instead.")]
    internal static partial void FailedToSweepDormantTarget(this ILogger<DormantTargetSweep> logger, string observerId, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Sweeping the durable subscriptions of dormant observers failed. The safety net of their queues finds the observers instead.")]
    internal static partial void FailedToSweepDormantTargets(this ILogger<DormantTargetSweep> logger, Exception exception);

    [LoggerMessage(LogLevel.Information, "Silos {Silos} left the cluster; {Observers} dormant observer(s) recorded clients served from them and were woken to drop them.")]
    internal static partial void DormantObserversSweptForDepartedSilos(this ILogger<DormantSiloDepartures> logger, string silos, int observers);

    [LoggerMessage(LogLevel.Warning, "Checking the cluster for departed silos failed.")]
    internal static partial void FailedToCheckForDepartedSilos(this ILogger<DormantSiloDepartures> logger, Exception exception);
}
