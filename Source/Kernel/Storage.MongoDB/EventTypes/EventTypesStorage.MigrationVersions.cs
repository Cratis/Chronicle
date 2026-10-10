// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Strings;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.EventTypes;

public partial class EventTypesStorage
{
    /// <inheritdoc/>
    public async Task RecordMigrationsVersion(EventTypeId eventTypeId, EventTypeMigrationsVersion version, IEnumerable<EventTypeMigrationDefinition> migrations)
    {
        // The empty set still has an identity, but an empty field name is not a portable storage key.
        var field = $"{nameof(EventType.MigrationVersions).ToCamelCase()}.{VersionKey(version)}";
        var definitions = migrations.Select(_ => new EventTypeMigration(_.FromGeneration, _.ToGeneration, BsonDocument.Parse(_.UpcastJmesPath.ToJsonString()), BsonDocument.Parse(_.DowncastJmesPath.ToJsonString()))).ToArray();
        var stored = new MigrationVersion(definitions, DateTimeOffset.UtcNow);
        var filter = Builders<EventType>.Filter.Eq(_ => _.Id, eventTypeId) & Builders<EventType>.Filter.Exists(field, false);
        await GetCollection().UpdateOneAsync(filter, Builders<EventType>.Update.Set(field, stored)).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<EventTypeMigrationsVersion, IEnumerable<EventTypeMigrationDefinition>>> GetMigrationsVersions(EventTypeId eventTypeId)
    {
        var stored = await GetCollection().Find(_ => _.Id == eventTypeId).FirstOrDefaultAsync().ConfigureAwait(false);

        return (stored?.MigrationVersions ?? new Dictionary<string, MigrationVersion>()).ToDictionary(
            _ => _.Key == "none" ? EventTypeMigrationsVersion.None : new EventTypeMigrationsVersion(_.Key),
            _ => (IEnumerable<EventTypeMigrationDefinition>)_.Value.Migrations.Select(migration => new EventTypeMigrationDefinition(
                migration.FromGeneration,
                migration.ToGeneration,
                [],
                JsonNode.Parse(migration.UpcastJmesPath.ToJson())!.AsObject(),
                JsonNode.Parse(migration.DowncastJmesPath.ToJson())!.AsObject())).ToArray());
    }

    static string VersionKey(EventTypeMigrationsVersion version) => version == EventTypeMigrationsVersion.None ? "none" : version.Value;
}
