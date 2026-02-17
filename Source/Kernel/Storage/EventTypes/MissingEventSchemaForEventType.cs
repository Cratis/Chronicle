// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventTypes;

/// <summary>
/// Exception that gets thrown when an event type is missing from the event types storage.
/// </summary>
/// <param name="eventStore"><see cref="EventStoreName"/> the schema for the event type is missing from.</param>
/// <param name="type">The <see cref="EventTypeId"/> missing.</param>
/// /// <param name="generation">The <see cref="EventTypeGeneration"/> that is missing.</param>
public class MissingEventSchemaForEventType(EventStoreName eventStore, EventTypeId type, EventTypeGeneration generation) :
    Exception($"Event type '{type}' with generation '{generation}' is missing from the event store '{eventStore}'");
