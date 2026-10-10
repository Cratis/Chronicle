// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes;

/// <summary>
/// Logging extensions for <see cref="EventTypesStorage"/>.
/// </summary>
internal static partial class EventTypesStorageLogging
{
    [LoggerMessage(LogLevel.Warning, "Giving up recording the migrations version for event type {EventTypeId} after repeated concurrent updates")]
    internal static partial void MigrationsVersionContended(this ILogger<EventTypesStorage> logger, EventTypeId eventTypeId);
}
