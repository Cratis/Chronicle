// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.EventSequences.Migrations;

/// <summary>
/// System event appended to the System event sequence when an event type is first registered.
/// </summary>
/// <param name="EventTypeId">The <see cref="Concepts.Events.EventTypeId"/> of the event type that was added.</param>
/// <param name="Generation">The initial <see cref="EventTypeGeneration"/> registered for the event type.</param>
/// <param name="Schema">The JSON schema of the initial generation.</param>
[EventType, AllEventStores]
public record EventTypeAdded(
    EventTypeId EventTypeId,
    EventTypeGeneration Generation,
    string Schema);