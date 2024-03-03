// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;

namespace Cratis.Kernel.Storage.EventTypes;

/// <summary>
/// Exception that gets thrown when an event type is missing from the event types storage.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MissingEventSchemaForEventType"/>.
/// </remarks>
/// <param name="eventStore"><see cref="EventStoreName"/> the event is missing from.</param>
/// <param name="type">The <see cref="EventTypeId"/> missing.</param>
/// <param name="generation">The <see cref="EventGeneration"/> that is missing.</param>
public class MissingEventSchemaForEventType(EventStoreName eventStore, EventTypeId type, EventGeneration generation) : Exception($"Event type '{type}' with generation '{generation}' is missing from the event store '{eventStore}'")
{
}
