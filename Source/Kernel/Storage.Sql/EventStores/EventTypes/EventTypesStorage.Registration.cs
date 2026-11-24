// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes;

public partial class EventTypesStorage
{
    static void EnsureSchemaChangesStillApply(EventType? observed, EventType? current, Dictionary<uint, string> changes)
    {
        foreach (var change in changes)
        {
            var previous = SchemaFor(observed, change.Key);
            var latest = SchemaFor(current, change.Key);
            if (latest != previous && latest != change.Value)
            {
                throw new DbUpdateConcurrencyException($"Generation '{change.Key}' changed during event type registration.");
            }
        }
    }

    static string? SchemaFor(EventType? type, uint generation) =>
        type is not null && type.Schemas.TryGetValue(generation, out var schema) ? schema : null;

    static Task<bool> TrySaveRegistration(EventStoreDbContext context, EventType? observed, EventType registration) =>
        observed is null
            ? TryInsertRegistration(context, registration)
            : TryUpdateRegistration(context, observed, registration);

    static async Task<bool> TryInsertRegistration(EventStoreDbContext context, EventType registration)
    {
        context.EventTypes.Add(registration);
        try
        {
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception error) when (StorageWriteConflicts.IsUnique(error))
        {
            // A competing registration won the insert. Re-read and merge instead of upserting over it.
            context.Entry(registration).State = EntityState.Detached;
            return false;
        }
    }

    static async Task<bool> TryUpdateRegistration(EventStoreDbContext context, EventType observed, EventType registration)
    {
        var expectedSchemas = observed.Schemas;
        var expectedMigrations = observed.MigrationsJson;
        return await context.EventTypes
            .Where(_ => _.Id == registration.Id && _.Schemas == expectedSchemas && _.MigrationsJson == expectedMigrations)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(_ => _.Owner, registration.Owner)
                .SetProperty(_ => _.Source, registration.Source)
                .SetProperty(_ => _.Visibility, registration.Visibility)
                .SetProperty(_ => _.Origin, registration.Origin)
                .SetProperty(_ => _.Tombstone, registration.Tombstone)
                .SetProperty(_ => _.Schemas, registration.Schemas)
                .SetProperty(_ => _.MigrationsJson, registration.MigrationsJson)) > 0;
    }

    async Task SaveRegistration(EventStoreDbContext context, EventType? observed, EventType registration)
    {
        // Only the generations changed by this registration may be reapplied after a conflict.
        // An unchanged incoming generation must not undo another replica's schema evolution.
        var changes = registration.Schemas
            .Where(_ => observed is null || !observed.Schemas.TryGetValue(_.Key, out var stored) || stored != _.Value)
            .ToDictionary();
        var migrationsChanged = observed?.MigrationsJson != registration.MigrationsJson;

        for (var attempt = 0; attempt < 5; attempt++)
        {
            registration.Schemas = MergeSchemas(observed?.Schemas ?? new Dictionary<uint, string>(), changes);
            if (!migrationsChanged && observed is not null)
            {
                registration.MigrationsJson = observed.MigrationsJson;
            }

            if (await TrySaveRegistration(context, observed, registration))
            {
                return;
            }

            var current = await context.EventTypes.AsNoTracking().FirstOrDefaultAsync(_ => _.Id == registration.Id);
            EnsureSchemaChangesStillApply(observed, current, changes);
            observed = current;
        }

        throw new DbUpdateConcurrencyException($"Could not register event type '{registration.Id}' after concurrent schema changes.");
    }
}
