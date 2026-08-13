// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.SequenceQueries;
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

        return definitions.Select(_ => _.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<Concepts.SequenceQueries.SequenceQueryDefinition>> ObserveAllFor(SequenceQueryOwner owner)
    {
        var subject = _subjects.GetOrAdd(owner.Value, _ => new ReplaySubject<IEnumerable<Concepts.SequenceQueries.SequenceQueryDefinition>>(1));
        _ = NotifyOwner(owner, subject);

        return subject;
    }

    /// <inheritdoc/>
    public async Task Save(Concepts.SequenceQueries.SequenceQueryDefinition definition)
    {
        await using var scope = await database.EventStore(eventStore);
        await scope.DbContext.SequenceQueries.Upsert(definition.ToSql());
        await scope.DbContext.SaveChangesAsync();
        await NotifyChange();
    }

    /// <inheritdoc/>
    public async Task Delete(SequenceQueryId id)
    {
        await using var scope = await database.EventStore(eventStore);
        var value = id.Value;
        var definition = await scope.DbContext.SequenceQueries.SingleOrDefaultAsync(_ => _.Id == value);
        if (definition is not null)
        {
            scope.DbContext.SequenceQueries.Remove(definition);
            await scope.DbContext.SaveChangesAsync();
            await NotifyChange();
        }
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
