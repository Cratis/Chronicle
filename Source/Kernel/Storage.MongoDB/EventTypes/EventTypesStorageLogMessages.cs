// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.MongoDB.EventTypes;

internal static partial class EventTypesStorageLogMessages
{
    [LoggerMessage(1, LogLevel.Debug, "Populating event schemas for event store '{EventStore}'")]
    internal static partial void Populating(this ILogger<EventTypesStorage> logger, EventStoreName eventStore);

    [LoggerMessage(2, LogLevel.Debug, "Registering event schema for '{Name}' (Id: {EventType} - Generation: {Generation}) for event store '{EventStore}'")]
    internal static partial void Registering(this ILogger<EventTypesStorage> logger, string name, EventTypeId eventType, EventGeneration generation, EventStoreName eventStore);
}
