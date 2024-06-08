// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Observation;
using Cratis.Chronicle.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

internal static partial class ObserversLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Registering observers")]
    internal static partial void RegisterObservers(this ILogger<ObserversRegistrar> logger);

    [LoggerMessage(1, LogLevel.Trace, "Registering observer with id '{ObserverId}' - friendly name {ObserverName}, for event sequence '{EventSequenceId}'")]
    internal static partial void RegisterObserver(this ILogger<ObserversRegistrar> logger, ObserverId observerId, ObserverName observerName, EventSequenceId eventSequenceId);
}
