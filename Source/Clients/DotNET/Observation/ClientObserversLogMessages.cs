// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Observation;

internal static partial class ClientObserversLogMessages
{
    [LoggerMessage(0, LogLevel.Trace, "Event of type {EventTypeId} was received for observer {ObserverId}")]
    internal static partial void EventReceived(this ILogger<ClientObservers> logger, EventTypeId eventTypeId, ObserverId observerId);

    [LoggerMessage(1, LogLevel.Warning, "Observer {ObserverId} does not exist.")]
    internal static partial void UnknownObserver(this ILogger<ClientObservers> logger, ObserverId observerId);
}
