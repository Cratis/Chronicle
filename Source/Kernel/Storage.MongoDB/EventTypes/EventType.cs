// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Cratis.Chronicle.Storage.MongoDB.Events.EventTypes;

/// <summary>
/// Represents the <see cref="EventTypeSchema"/> for MongoDB storage purpose.
/// </summary>
/// <param name="Id"><see cref="EventTypeId">Unique identifier</see> of the event type.</param>
/// <param name="Owner"><see cref="EventTypeOwner">Owner</see> of the event type.</param>
/// <param name="Source"><see cref="EventTypeSource">Source</see> of the event type.</param>
/// <param name="Tombstone">Whether or not the event type is a tombstone event.</param>
/// <param name="Schemas">A dictionary of <see cref="EventTypeGeneration">event type generations</see> and their corresponding JSON schemas.</param>
/// <param name="Migrations">Collection of migration definitions between generations.</param>
/// <param name="Visibility"><see cref="EventTypeVisibility">Visibility</see> of the event type. Documents stored before visibility existed read back as unspecified.</param>
/// <param name="Origin">Name of the event store the event type originates from when it is not the registering one, otherwise empty.</param>
public record EventType(
    EventTypeId Id,
    EventTypeOwner Owner,
    EventTypeSource Source,
    bool Tombstone,
    IDictionary<string, BsonDocument> Schemas,
    IEnumerable<EventTypeMigration>? Migrations = null,
    EventTypeVisibility Visibility = EventTypeVisibility.Unspecified,
    string Origin = "")
{
    /// <summary>
    /// Gets the immutable migration definitions keyed by their content-addressed version.
    /// </summary>
    [BsonIgnoreIfNull]
    public IDictionary<string, MigrationVersion>? MigrationVersions { get; init; }
}
