// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

internal static partial class ObserverDormancyLogging
{
    [LoggerMessage(LogLevel.Debug, "Releasing the observer: it has been subscribed with nothing to do for {DormantPeriod}")]
    internal static partial void ReleasingDormantObserver(this ILogger<Observer> logger, TimeSpan dormantPeriod);

    [LoggerMessage(LogLevel.Warning, "The appended events queue could not keep the subscription of the observer that is being released; it leaves the ordinary way")]
    internal static partial void KeepingDormantEntryFailed(this ILogger<Observer> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Resuming the observer from its durable subscription failed; it catches up from its cursor")]
    internal static partial void ResumeFromDormancyFailed(this ILogger<Observer> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "The appended events queue could not drop the stored subscription that could not be restored")]
    internal static partial void ForgettingDormantEntryFailed(this ILogger<Observer> logger, Exception exception);
}
