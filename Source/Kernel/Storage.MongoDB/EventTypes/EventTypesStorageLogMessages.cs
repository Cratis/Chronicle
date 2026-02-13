// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Events.EventTypes;

internal static partial class EventTypesStorageLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Populating event schemas for event store '{EventStore}'")]
    internal static partial void Populating(this ILogger<EventTypesStorage> logger, EventStoreName eventStore);

    [LoggerMessage(LogLevel.Debug, "Registering event schema for (Id: {EventType} - Generation: {Generation}) for event store '{EventStore}'")]
    internal static partial void Registering(this ILogger<EventTypesStorage> logger, EventTypeId eventType, EventTypeGeneration generation, EventStoreName eventStore);
}
