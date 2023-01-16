// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Observation;

public static partial class ObserversLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Registering observers")]
    internal static partial void RegisterObservers(this ILogger<Observers> logger);

    [LoggerMessage(1, LogLevel.Trace, "Registering observer with id '{ObserverId}' - friendly name {ObserverName}, for event sequence '{EventSequenceId}'")]
    internal static partial void RegisterObserver(this ILogger<Observers> logger, ObserverId observerId, ObserverName observerName, EventSequenceId eventSequenceId);
}
