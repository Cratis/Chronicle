// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;
using Microsoft.EntityFrameworkCore;

using RowEncoding = Cratis.Chronicle.Storage.Observation.ObserverDefinitionChangeRows;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers;

/// <summary>
/// Stores one definition-change family with atomic conditional updates, never a read-then-save lifecycle update.
/// </summary>
/// <typeparam name="T">The payload type.</typeparam>
/// <param name="open">Opens the namespace's primary database.</param>
public class ObserverDefinitionChangeRows<T>(Func<Task<DbContextScope<NamespaceDbContext>>> open) : IObserverDefinitionChangeRows<T>
{
    /// <inheritdoc/>
    public async Task<ObserverDefinitionChangeRow<T>?> Get(ObserverDefinitionChangeRowKey key)
    {
        await using var scope = await open();
        var id = RowEncoding.Id<T>(key);
        var entry = await scope.DbContext.Set<ObserverDefinitionChangeEntry>().AsNoTracking().SingleOrDefaultAsync(row => row.Id == id);

        return entry is null ? null : RowEncoding.Deserialize<T>(entry.Data);
    }

    /// <inheritdoc/>
    public async Task<bool> TryInsert(ObserverDefinitionChangeRow<T> row)
    {
        if (row.Version != 0 || !RowEncoding.IsValid(row)) return false;
        await using var scope = await open();
        scope.DbContext.Set<ObserverDefinitionChangeEntry>().Add(new()
        {
            Id = RowEncoding.Id<T>(row.Key),
            Observer = RowEncoding.Observer(row.Key.Observer),
            Family = typeof(T).Name,
            Revision = row.Key.Revision.Value,
            Version = row.Version,
            State = (int)row.State,
            Position = row.Position?.Value,
            Data = RowEncoding.Serialize(row)
        });
        try
        {
            await scope.DbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception error) when (StorageWriteConflicts.IsUnique(error))
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> TryReplace(ObserverDefinitionChangeRow<T> row, long expectedVersion)
    {
        if (!RowEncoding.IsReplacement(row, expectedVersion)) return false;
        await using var scope = await open();
        var id = RowEncoding.Id<T>(row.Key);
        var state = (int)row.State;
        decimal? position = row.Position?.Value;
        var data = RowEncoding.Serialize(row);
        var changed = await scope.DbContext.Set<ObserverDefinitionChangeEntry>()
            .Where(entry => entry.Id == id && entry.Version == expectedVersion && entry.State < (int)ObserverDefinitionChangeRowState.Terminal && entry.State <= state &&
                (entry.Position == null || (position != null && entry.Position <= position)))
            .ExecuteUpdateAsync(set => set.SetProperty(entry => entry.Version, row.Version)
                .SetProperty(entry => entry.State, state).SetProperty(entry => entry.Position, position).SetProperty(entry => entry.Data, data));

        return changed == 1;
    }

    /// <inheritdoc/>
    public async Task<ObserverDefinitionChangePage<T>> ReadPage(ObserverKey observer, ObserverTypeSetRevision? revision, string? after, int limit)
    {
        if (limit <= 0) return new([], null);
        await using var scope = await open();
        var observerId = RowEncoding.Observer(observer);
        var family = typeof(T).Name;
        var query = scope.DbContext.Set<ObserverDefinitionChangeEntry>().AsNoTracking().Where(row => row.Observer == observerId && row.Family == family);
        if (revision is not null) query = query.Where(row => row.Revision == revision.Value);
        if (after is not null) query = query.Where(row => row.Id.CompareTo(after) > 0);
        var entries = await query.OrderBy(row => row.Id).Take(Math.Min(limit, 1000)).ToListAsync();

        return new(entries.Select(entry => RowEncoding.Deserialize<T>(entry.Data)).ToArray(), entries.Count == 0 ? null : entries[^1].Id);
    }

    /// <inheritdoc/>
    public async Task<bool> TryDelete(ObserverDefinitionChangeRowKey key, long expectedVersion)
    {
        await using var scope = await open();
        var id = RowEncoding.Id<T>(key);
        var deleted = await scope.DbContext.Set<ObserverDefinitionChangeEntry>().Where(row => row.Id == id && row.Version == expectedVersion).ExecuteDeleteAsync();

        return deleted == 1;
    }
}
