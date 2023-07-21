// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Observation.Clients;

internal static partial class ClientObserversLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Registering observers")]
    internal static partial void RegisterObservers(this ILogger<ClientObservers> logger);

    [LoggerMessage(1, LogLevel.Trace, "Registering observer with id '{ObserverId}' - friendly name {ObserverName}, for event sequence '{EventSequenceId}'")]
    internal static partial void RegisterObserver(this ILogger<ClientObservers> logger, ObserverId observerId, ObserverName observerName, EventSequenceId eventSequenceId);
}
