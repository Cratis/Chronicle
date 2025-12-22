// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers;

/// <summary>
/// Represents an implementation of <see cref="IEventTypesStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="namespace">The name of the namespace.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class ObserverStateStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, IDatabase database) : IObserverStateStorage
{
    /// <inheritdoc/>
    public ISubject<IEnumerable<Observation.ObserverState>> ObserveAll() => throw new NotImplementedException();

    /// <inheritdoc/>
    public async Task<IEnumerable<Observation.ObserverState>> GetAll()
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var observers = await scope.DbContext.Observers.ToListAsync();
        return observers.Select(observer => observer.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public async Task<Observation.ObserverState> Get(ObserverId observerId)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var observer = await scope.DbContext.Observers
            .Where(observer => observer.Id == observerId)
            .Select(observer => observer.ToKernel())
            .FirstOrDefaultAsync() ?? Observation.ObserverState.Empty;
        return observer!;
    }

    /// <inheritdoc/>
    public async Task Save(Observation.ObserverState state)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var entity = state.ToSql();
        await scope.DbContext.Observers.Upsert(entity);
        await scope.DbContext.SaveChangesAsync();
    }
}
