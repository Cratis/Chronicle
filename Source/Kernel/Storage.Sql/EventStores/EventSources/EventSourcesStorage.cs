// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSources;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventSources;

/// <summary>
/// Represents the SQL implementation of <see cref="IEventSourcesStorage"/>.
/// </summary>
/// <param name="eventStore">The <see cref="EventStoreName"/> the storage is for.</param>
/// <param name="database">The <see cref="IDatabase"/> to use.</param>
public class EventSourcesStorage(EventStoreName eventStore, IDatabase database) : IEventSourcesStorage, IDisposable
{
    readonly ReplaySubject<IEnumerable<Concepts.EventSources.EventSourceDefinition>> _subject = new(1);

    /// <inheritdoc/>
    public async Task<IEnumerable<Concepts.EventSources.EventSourceDefinition>> GetAll()
    {
        await using var scope = await database.EventStore(eventStore);
        var definitions = await scope.DbContext.EventSourceDefinitions.ToListAsync();
        return definitions.Select(definition => definition.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<Concepts.EventSources.EventSourceDefinition>> ObserveAll() => _subject;

    /// <inheritdoc/>
    public async Task<Concepts.EventSources.EventSourceDefinition?> Find(EventSourceName name)
    {
        await using var scope = await database.EventStore(eventStore);
        var id = name.Value;
        var definition = await scope.DbContext.EventSourceDefinitions.SingleOrDefaultAsync(_ => _.Id == id);
        return definition?.ToKernel();
    }

    /// <inheritdoc/>
    public async Task Save(Concepts.EventSources.EventSourceDefinition definition)
    {
        await using (var scope = await database.EventStore(eventStore))
        {
            await scope.DbContext.EventSourceDefinitions.Upsert(definition.ToSql());
            await scope.DbContext.SaveChangesAsync();
        }

        _subject.OnNext(await GetAll());
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _subject.Dispose();
        GC.SuppressFinalize(this);
    }
}
