// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.EventStores;
using Cratis.Types;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Represents an implementation of <see cref="IClusterStorage"/> for SQL.
/// </summary>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
/// <param name="sinkFactories"><see cref="IInstancesOf{T}"/> for getting all <see cref="ISinkFactory"/> instances.</param>
/// <param name="jobTypes">The <see cref="IJobTypes"/> that knows about job types.</param>
/// <param name="jsonSerializerOptions">The configured <see cref="JsonSerializerOptions"/> including all concept converters.</param>
public class ClusterStorage(IDatabase database, IInstancesOf<ISinkFactory> sinkFactories, IJobTypes jobTypes, JsonSerializerOptions jsonSerializerOptions) : IClusterStorage, IDisposable
{
    readonly BehaviorSubject<IEnumerable<EventStoreName>> _eventStoresSubject = new([]);
    int _eventStoresInitialized;

    /// <inheritdoc/>
    public async Task<IEnumerable<EventStoreName>> GetEventStores()
    {
        await using var scope = await database.Cluster();
        var names = await scope.DbContext.EventStores.Select(es => es.Name).ToListAsync();
        return names.Select(name => (EventStoreName)name);
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<EventStoreName>> ObserveEventStores()
    {
        if (Interlocked.CompareExchange(ref _eventStoresInitialized, 1, 0) == 0)
        {
            _ = PushEventStoresToSubjectAsync();
        }
        return _eventStoresSubject;
    }

    /// <inheritdoc/>
    public IEventStoreStorage CreateStorageForEventStore(EventStoreName eventStore, SinksFactory sinksFactory)
    {
        return new EventStoreStorage(eventStore, database, sinkFactories, jobTypes, jsonSerializerOptions);
    }

    /// <inheritdoc/>
    public async Task SaveEventStore(EventStoreName eventStore)
    {
        await using var scope = await database.Cluster();
        await scope.DbContext.EventStores.Upsert(new EventStore { Name = eventStore });
        await scope.DbContext.SaveChangesAsync();
        await PushEventStoresToSubjectAsync();
    }

    /// <inheritdoc/>
    public void Dispose() => _eventStoresSubject.Dispose();

    async Task PushEventStoresToSubjectAsync()
    {
        var stores = await GetEventStores();
        _eventStoresSubject.OnNext(stores);
    }
}
