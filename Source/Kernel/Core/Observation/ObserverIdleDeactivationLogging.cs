// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

internal static partial class ObserverIdleDeactivationLogging
{
    [LoggerMessage(LogLevel.Debug, "Releasing the observer: it has been idle without a subscription for {IdlePeriod}")]
    internal static partial void ReleasingIdleObserver(this ILogger<Observer> logger, TimeSpan idlePeriod);

    [LoggerMessage(LogLevel.Debug, "The observer was released as idle but is still active and has work again, so its watchdog runs as before")]
    internal static partial void IdleObserverBusyAgain(this ILogger<Observer> logger);
}
