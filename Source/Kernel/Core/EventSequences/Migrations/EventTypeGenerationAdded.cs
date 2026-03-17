// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.EventSequences.Migrations;

/// <summary>
/// System event appended to the System event sequence when a new generation of an event type is registered.
/// A reactor handles this event and starts a migration job to update existing stored events
/// with content migrated to the new generation.
/// </summary>
/// <param name="EventTypeId">The <see cref="Concepts.Events.EventTypeId"/> of the event type that received a new generation.</param>
/// <param name="Generation">The new <see cref="EventTypeGeneration"/> that was added.</param>
/// <param name="Schema">The JSON schema of the new generation.</param>
[EventType, AllEventStores]
public record EventTypeGenerationAdded(
    EventTypeId EventTypeId,
    EventTypeGeneration Generation,
    string Schema);
