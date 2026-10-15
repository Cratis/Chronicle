// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;

/// <summary>
/// Stores trigger definitions in the event-store database with revision compare-and-set.
/// </summary>
/// <param name="open">Opens an event-store context for each operation.</param>
public class TriggerDefinitionsStorage(Func<Task<DbContextScope<EventStoreDbContext>>> open) : ITriggerDefinitionsStorage
{
    /// <inheritdoc/>
    public async Task<TriggerDefinition?> Get(TriggerId id)
    {
        await using var scope = await open();
        var hash = StorageIdentity.Hash(id.Value);
        var row = await scope.DbContext.Set<TriggerDefinitionEntry>().AsNoTracking().SingleOrDefaultAsync(row => row.Id == hash);
        return row is null ? null : FromEntry(row);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TriggerDefinition>> GetAll()
    {
        await using var scope = await open();
        var rows = await scope.DbContext.Set<TriggerDefinitionEntry>().AsNoTracking().ToListAsync();
        return rows.Select(FromEntry).ToArray();
    }

    /// <inheritdoc/>
    public async Task<bool> TrySave(TriggerDefinition definition, long? expectedRevision)
    {
        var hash = StorageIdentity.Hash(definition.Id.Value);
        await using var scope = await open();
        if (expectedRevision.HasValue)
        {
            return await scope.DbContext.Set<TriggerDefinitionEntry>().Where(row => row.Id == hash && row.Revision == expectedRevision.Value)
                .ExecuteUpdateAsync(set => set.SetProperty(row => row.Name, definition.Name).SetProperty(row => row.Schema, definition.Schema).SetProperty(row => row.Revision, definition.Revision)) == 1;
        }

        try
        {
            scope.DbContext.Set<TriggerDefinitionEntry>().Add(new TriggerDefinitionEntry { Id = hash, Trigger = definition.Id.Value, Name = definition.Name, Schema = definition.Schema, Revision = definition.Revision });
            await scope.DbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception error) when (StorageWriteConflicts.IsUnique(error))
        {
            return false;
        }
    }

    static TriggerDefinition FromEntry(TriggerDefinitionEntry row) => new(new TriggerId(row.Trigger), row.Name, row.Schema, row.Revision);
}
