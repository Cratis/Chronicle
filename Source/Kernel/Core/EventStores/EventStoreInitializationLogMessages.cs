// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventStores;

internal static partial class EventStoreInitializationLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Failed to initialize event store '{EventStore}' before listing it")]
    internal static partial void EventStoreInitializationFailed(this ILogger<EventStoreInitialization> logger, EventStoreName eventStore, Exception exception);
}
