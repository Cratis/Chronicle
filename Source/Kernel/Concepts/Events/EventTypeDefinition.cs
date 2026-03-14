// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events;

/// <summary>
/// Represents the complete definition of an event type including all generations and migrations.
/// </summary>
/// <param name="Id">The unique <see cref="EventTypeId"/>.</param>
/// <param name="Owner">The <see cref="EventTypeOwner"/>.</param>
/// <param name="Tombstone">Whether this is a tombstone event type.</param>
/// <param name="Generations">All generations of this event type.</param>
/// <param name="Migrations">All migrations between generations.</param>
public record EventTypeDefinition(
    EventTypeId Id,
    EventTypeOwner Owner,
    bool Tombstone,
    IEnumerable<EventTypeGenerationDefinition> Generations,
    IEnumerable<EventTypeMigrationDefinition> Migrations);
