// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes;

public partial class EventTypesStorage
{
    readonly Dictionary<EventTypeId, Dictionary<EventTypeMigrationsVersion, IEnumerable<EventTypeMigrationDefinition>>> _migrationVersions = [];

    /// <inheritdoc/>
    public Task RecordMigrationsVersion(EventTypeId eventTypeId, EventTypeMigrationsVersion version, IEnumerable<EventTypeMigrationDefinition> migrations)
    {
        lock (_publishing)
        {
            if (!_migrationVersions.TryGetValue(eventTypeId, out var versions))
            {
                _migrationVersions[eventTypeId] = versions = [];
            }
            versions.TryAdd(version, CloneMigrations(migrations));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyDictionary<EventTypeMigrationsVersion, IEnumerable<EventTypeMigrationDefinition>>> GetMigrationsVersions(EventTypeId eventTypeId)
    {
        lock (_publishing)
        {
            return Task.FromResult<IReadOnlyDictionary<EventTypeMigrationsVersion, IEnumerable<EventTypeMigrationDefinition>>>(
                _migrationVersions.TryGetValue(eventTypeId, out var versions)
                    ? versions.ToDictionary(_ => _.Key, _ => (IEnumerable<EventTypeMigrationDefinition>)CloneMigrations(_.Value)) : new Dictionary<EventTypeMigrationsVersion, IEnumerable<EventTypeMigrationDefinition>>());
        }
    }

    static EventTypeMigrationDefinition[] CloneMigrations(IEnumerable<EventTypeMigrationDefinition> migrations) =>
        migrations.Select(_ => _ with
        {
            Operations = _.Operations.ToArray(),
            UpcastJmesPath = _.UpcastJmesPath.DeepClone().AsObject(),
            DowncastJmesPath = _.DowncastJmesPath.DeepClone().AsObject()
        }).ToArray();
}
