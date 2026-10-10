// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes;

public partial class EventTypesStorage
{
    static readonly JsonSerializerOptions _migrationVersionOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc/>
    public async Task RecordMigrationsVersion(EventTypeId eventTypeId, EventTypeMigrationsVersion version, IEnumerable<EventTypeMigrationDefinition> migrations)
    {
        var definitions = migrations.ToArray();
        await using var scope = await database.EventStore(eventStore);

        // Compare-and-swap retains both versions if different silos record concurrently.
        while (true)
        {
            var stored = await scope.DbContext.EventTypes.AsNoTracking().FirstAsync(_ => _.Id == eventTypeId);
            var observed = stored.MigrationVersionsJson;
            var versions = JsonSerializer.Deserialize<Dictionary<string, EventTypeMigrationDefinition[]>>(observed, _migrationVersionOptions)!;
            if (!versions.TryAdd(version.Value, definitions))
            {
                return;
            }

            var merged = JsonSerializer.Serialize(versions, _migrationVersionOptions);
            var rows = await scope.DbContext.EventTypes.Where(_ => _.Id == eventTypeId && _.MigrationVersionsJson == observed)
                .ExecuteUpdateAsync(setters => setters.SetProperty(_ => _.MigrationVersionsJson, merged));
            if (rows == 1)
            {
                return;
            }
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<EventTypeMigrationsVersion, IEnumerable<EventTypeMigrationDefinition>>> GetMigrationsVersions(EventTypeId eventTypeId)
    {
        await using var scope = await database.EventStore(eventStore);
        var stored = await scope.DbContext.EventTypes.AsNoTracking().FirstOrDefaultAsync(_ => _.Id == eventTypeId);
        var versions = JsonSerializer.Deserialize<Dictionary<string, EventTypeMigrationDefinition[]>>(stored?.MigrationVersionsJson ?? "{}", _migrationVersionOptions)!;

        return versions.ToDictionary(_ => new EventTypeMigrationsVersion(_.Key), _ => (IEnumerable<EventTypeMigrationDefinition>)_.Value);
    }
}
