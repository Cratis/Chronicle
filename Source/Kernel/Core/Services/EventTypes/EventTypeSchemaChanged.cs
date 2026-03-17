// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Events;

/// <summary>
/// The exception that gets thrown when an event type's schema has changed compared to what is already registered.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventTypeSchemaChanged"/> class.
/// </remarks>
/// <param name="eventTypeId">The identifier of the event type.</param>
/// <param name="generation">The generation whose schema has changed.</param>
public class EventTypeSchemaChanged(string eventTypeId, uint generation)
    : Exception($"Event type '{eventTypeId}' at generation {generation} has a schema that differs from the already registered schema. Schema changes are not allowed without creating a new generation.");
