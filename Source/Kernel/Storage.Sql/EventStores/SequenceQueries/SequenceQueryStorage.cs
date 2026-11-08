// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.SequenceQueries;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.SequenceQueries;

/// <summary>
/// Represents a <see cref="ISequenceQueryStorage"/> for saved event sequence queries in SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class SequenceQueryStorage(EventStoreName eventStore, IDatabase database) : ISequenceQueryStorage, IDisposable
{
    readonly ConcurrentDictionary<string, ReplaySubject<IEnumerable<Concepts.SequenceQueries.SequenceQueryDefinition>>> _subjects = new();

    /// <inheritdoc/>
    public async Task<IEnumerable<Concepts.SequenceQueries.SequenceQueryDefinition>> GetAllFor(SequenceQueryOwner owner)
    {
        await using var scope = await database.EventStore(eventStore);
        var ownerValue = owner.Value;
        var definitions = await scope.DbContext.SequenceQueries
            .Where(_ => _.Scope == SequenceQueryScope.Everyone || _.Owner == ownerValue)
            .OrderBy(_ => _.Name)
            .ToListAsync();

        return definitions
            .Where(_ => _.Scope == SequenceQueryScope.Everyone || string.Equals(_.Owner, ownerValue, StringComparison.Ordinal))
            .Select(_ => _.ToKernel())
            .ToArray();
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<Concepts.SequenceQueries.SequenceQueryDefinition>> ObserveAllFor(SequenceQueryOwner owner)
    {
        var subject = _subjects.GetOrAdd(owner.Value, _ => new ReplaySubject<IEnumerable<Concepts.SequenceQueries.SequenceQueryDefinition>>(1));
        _ = NotifyOwner(owner, subject);

        return subject;
    }

    /// <inheritdoc/>
    public async Task<Concepts.SequenceQueries.SequenceQueryDefinition?> Get(SequenceQueryId id)
    {
        await using var scope = await database.EventStore(eventStore);
        var value = id.Value;
        var definition = await scope.DbContext.SequenceQueries.AsNoTracking().SingleOrDefaultAsync(_ => _.Id == value);

        return definition?.ToKernel();
    }

    /// <inheritdoc/>
    public async Task<SequenceQueryFolderDefinition?> GetFolder(SequenceQueryFolderId id)
    {
        await using var scope = await database.EventStore(eventStore);
        var value = id.Value;
        var folder = await scope.DbContext.SequenceQueryFolders.AsNoTracking().SingleOrDefaultAsync(_ => _.Id == value);

        return folder?.ToKernel();
    }

    /// <inheritdoc/>
    public async Task<SequenceQueryWriteOutcome> Insert(Concepts.SequenceQueries.SequenceQueryDefinition definition)
    {
        var outcome = await InsertOnce(context => context.SequenceQueries.Add(definition.ToSql()), async () => await Get(definition.Id) is not null);
        if (outcome == SequenceQueryWriteOutcome.Applied) await NotifyChange();

        return outcome;
    }

    /// <inheritdoc/>
    public async Task<SequenceQueryWriteOutcome> Update(Concepts.SequenceQueries.SequenceQueryDefinition definition, SequenceQueryOwner expectedOwner)
    {
        await using var scope = await database.EventStore(eventStore);
        var id = definition.Id.Value;
        var sql = definition.ToSql();

        // The owner is matched exactly and never written, so an update keeps the owner it is stored under.
        var changed = await scope.DbContext.SequenceQueries
            .Where(_ => _.Id == id)
            .WhereExact(scope.DbContext, _ => _.Owner, expectedOwner.Value)
            .ExecuteUpdateAsync(set => set
                .SetProperty(_ => _.Name, sql.Name)
                .SetProperty(_ => _.Scope, sql.Scope)
                .SetProperty(_ => _.Folder, sql.Folder)
                .SetProperty(_ => _.Namespace, sql.Namespace)
                .SetProperty(_ => _.EventSequenceId, sql.EventSequenceId)
                .SetProperty(_ => _.Filter, sql.Filter)
                .SetProperty(_ => _.SortBy, sql.SortBy)
                .SetProperty(_ => _.Descending, sql.Descending));
        if (changed == 0) return SequenceQueryWriteOutcome.Conflict;

        await NotifyChange();
        return SequenceQueryWriteOutcome.Applied;
    }

    /// <inheritdoc/>
    public async Task<SequenceQueryWriteOutcome> Delete(SequenceQueryId id, SequenceQueryOwner expectedOwner)
    {
        await using var scope = await database.EventStore(eventStore);
        var value = id.Value;
        var deleted = await scope.DbContext.SequenceQueries
            .Where(_ => _.Id == value)
            .WhereExact(scope.DbContext, _ => _.Owner, expectedOwner.Value)
            .ExecuteDeleteAsync();
        if (deleted > 0)
        {
            await NotifyChange();
            return SequenceQueryWriteOutcome.Applied;
        }

        return await Get(id) is null ? SequenceQueryWriteOutcome.Applied : SequenceQueryWriteOutcome.Conflict;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<SequenceQueryFolderDefinition>> GetAllFoldersFor(SequenceQueryOwner owner)
    {
        await using var scope = await database.EventStore(eventStore);
        var ownerValue = owner.Value;
        var folders = await scope.DbContext.SequenceQueryFolders
            .Where(_ => _.Scope == SequenceQueryScope.Everyone || _.Owner == ownerValue)
            .OrderBy(_ => _.Path)
            .ToListAsync();

        return folders
            .Where(_ => _.Scope == SequenceQueryScope.Everyone || string.Equals(_.Owner, ownerValue, StringComparison.Ordinal))
            .Select(_ => _.ToKernel())
            .ToArray();
    }

    /// <inheritdoc/>
    public Task<SequenceQueryWriteOutcome> InsertFolder(SequenceQueryFolderDefinition definition) =>
        InsertOnce(context => context.SequenceQueryFolders.Add(definition.ToSql()), async () => await GetFolder(definition.Id) is not null);

    /// <inheritdoc/>
    public async Task<SequenceQueryWriteOutcome> UpdateFolder(SequenceQueryFolderDefinition definition, SequenceQueryOwner expectedOwner)
    {
        await using var scope = await database.EventStore(eventStore);
        var id = definition.Id.Value;
        var sql = definition.ToSql();

        // The owner is matched exactly and never written, so an update keeps the owner it is stored under.
        var changed = await scope.DbContext.SequenceQueryFolders
            .Where(_ => _.Id == id)
            .WhereExact(scope.DbContext, _ => _.Owner, expectedOwner.Value)
            .ExecuteUpdateAsync(set => set
                .SetProperty(_ => _.Scope, sql.Scope)
                .SetProperty(_ => _.Namespace, sql.Namespace)
                .SetProperty(_ => _.Path, sql.Path));

        return changed == 0 ? SequenceQueryWriteOutcome.Conflict : SequenceQueryWriteOutcome.Applied;
    }

    /// <inheritdoc/>
    public async Task<SequenceQueryWriteOutcome> DeleteFolder(SequenceQueryFolderId id, SequenceQueryOwner expectedOwner)
    {
        await using var scope = await database.EventStore(eventStore);
        var value = id.Value;
        var deleted = await scope.DbContext.SequenceQueryFolders
            .Where(_ => _.Id == value)
            .WhereExact(scope.DbContext, _ => _.Owner, expectedOwner.Value)
            .ExecuteDeleteAsync();
        if (deleted > 0) return SequenceQueryWriteOutcome.Applied;

        return await GetFolder(id) is null ? SequenceQueryWriteOutcome.Applied : SequenceQueryWriteOutcome.Conflict;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var subject in _subjects.Values)
        {
            subject.Dispose();
        }

        _subjects.Clear();
        GC.SuppressFinalize(this);
    }

    async Task<SequenceQueryWriteOutcome> InsertOnce(Action<EventStoreDbContext> add, Func<Task<bool>> isStored)
    {
        try
        {
            await using var scope = await database.EventStore(eventStore);
            add(scope.DbContext);
            await scope.DbContext.SaveChangesAsync();
            return SequenceQueryWriteOutcome.Applied;
        }
        catch (DbUpdateException)
        {
            // A refused insert is only a conflict when the identifier is stored now; anything else is a real failure.
            if (await isStored()) return SequenceQueryWriteOutcome.Conflict;
            throw;
        }
    }

    async Task NotifyOwner(SequenceQueryOwner owner, ReplaySubject<IEnumerable<Concepts.SequenceQueries.SequenceQueryDefinition>> subject) =>
        subject.OnNext(await GetAllFor(owner));

    async Task NotifyChange()
    {
        foreach (var (owner, subject) in _subjects)
        {
            subject.OnNext(await GetAllFor(owner));
        }
    }
}
