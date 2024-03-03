// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Observation;

internal static partial class ObserversLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Registering observer '{Name}'")]
    internal static partial void RegisteringObservers(this ILogger<Observers> logger, ObserverName Name);

    [LoggerMessage(1, LogLevel.Trace, "Discover all observers")]
    internal static partial void DiscoverAllObservers(this ILogger<Observers> logger);

    [LoggerMessage(2, LogLevel.Trace, "Event of type {EventTypeId} was received for observer {ObserverId}")]
    internal static partial void EventReceived(this ILogger<Observers> logger, EventTypeId eventTypeId, ObserverId observerId);
}
