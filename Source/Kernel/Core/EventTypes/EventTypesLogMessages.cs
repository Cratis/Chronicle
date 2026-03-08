// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventTypes;

internal static partial class EventTypesLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Discovering and registering {EventTypeCount} event types for event store '{EventStore}'")]
    internal static partial void DiscoveringAndRegistering(this ILogger<EventTypes> logger, EventStoreName eventStore, int eventTypeCount);
}
