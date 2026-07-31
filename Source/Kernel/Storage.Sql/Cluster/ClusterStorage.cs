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
/// <remarks>
/// Changes fan out over an internal subject that is never handed to a caller. Every caller of
/// <see cref="ObserveEventStores"/> gets its own subject, because callers complete the subject they are given when
/// their connection goes away (see <see cref="Reactive.ObservableExtensions.CompletedBy{TResult}"/>) and completing a
/// shared subject would silently end event-store observation for every other caller in the process.
/// </remarks>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
/// <param name="sinkFactories"><see cref="IInstancesOf{T}"/> for getting all <see cref="ISinkFactory"/> instances.</param>
/// <param name="jobTypes">The <see cref="IJobTypes"/> that knows about job types.</param>
/// <param name="jsonSerializerOptions">The configured <see cref="JsonSerializerOptions"/> including all concept converters.</param>
public class ClusterStorage(IDatabase database, IInstancesOf<ISinkFactory> sinkFactories, IJobTypes jobTypes, JsonSerializerOptions jsonSerializerOptions) : IClusterStorage, IDisposable
{
    readonly Subject<IEnumerable<EventStoreName>> _eventStoresSubject = new();

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
        var subject = new ReplaySubject<IEnumerable<EventStoreName>>(1);

        var subscription = _eventStoresSubject.Subscribe(subject.OnNext);
        subject.Subscribe(_ => { }, _ => { }, subscription.Dispose);

        // Seed the caller's own subject with the current state. The fetch is asynchronous, so the value lands
        // after this method returns - the ReplaySubject holds on to it until the caller subscribes.
        _ = PushEventStoresToSubjectAsync(subject);

        return subject;
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
        await PushEventStoresToSubjectAsync(_eventStoresSubject);
    }

    /// <inheritdoc/>
    public void Dispose() => _eventStoresSubject.Dispose();

    async Task PushEventStoresToSubjectAsync(IObserver<IEnumerable<EventStoreName>> observer)
    {
        var stores = await GetEventStores();
        observer.OnNext(stores);
    }
}
