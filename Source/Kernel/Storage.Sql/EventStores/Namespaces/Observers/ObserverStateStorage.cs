// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers;

/// <summary>
/// Represents an implementation of <see cref="IObserverStateStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="namespace">The name of the namespace.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class ObserverStateStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, IDatabase database) : IObserverStateStorage, IDisposable
{
    readonly ReplaySubject<IEnumerable<Observation.ObserverState>> _subject = new(1);

    /// <inheritdoc/>
    public async Task<IEnumerable<Observation.ObserverState>> GetAll()
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var observers = await scope.DbContext.Observers.ToListAsync();
        var result = observers.Select(observer => observer.ToKernel()).ToArray();

        // Ensure the subject has initial data for first subscribers
        if (!_subject.HasObservers)
        {
            _subject.OnNext(result);
        }

        return result;
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<Observation.ObserverState>> ObserveAll()
    {
        // Trigger initial data load when first subscribed
        _ = GetAll();
        return _subject;
    }

    /// <inheritdoc/>
    public async Task<Observation.ObserverState> Get(ObserverId observerId)
    {
        // KeyHelper.Parse decodes an empty leading segment of a grain key as a null ObserverId
        // (the constructor accepts it as null!). The MongoDB sink quietly returns Empty in that
        // case because Match on null matches nothing; the SQL path used to NRE on observerId.Value
        // and abort grain activation. Match the MongoDB contract here so storage choice never
        // changes whether a grain can re-activate.
        if (observerId is null || string.IsNullOrEmpty(observerId.Value))
        {
            return Observation.ObserverState.Empty;
        }

        await using var scope = await database.Namespace(eventStore, @namespace);
        var observerIdValue = observerId.Value;
        return await scope.DbContext.Observers
            .Where(observer => observer.Id == observerIdValue)
            .Select(observer => observer.ToKernel())
            .FirstOrDefaultAsync() ?? Observation.ObserverState.Empty;
    }

    /// <inheritdoc/>
    public async Task Save(Observation.ObserverState state)
    {
        // EF change-tracking rejects a null primary key. Mirror the Get path tolerance: an
        // observer whose grain key decoded to an empty ObserverId has no real identity to
        // persist, and MongoDB's ReplaceOneAsync(IsUpsert: true) silently accepts the same
        // input. Returning here keeps storage-layer behavior aligned across backends.
        if (state.Identifier is null || string.IsNullOrEmpty(state.Identifier.Value))
        {
            return;
        }

        await using var scope = await database.Namespace(eventStore, @namespace);
        var entity = state.ToSql();
        await scope.DbContext.Observers.Upsert(entity);
        await scope.DbContext.SaveChangesAsync();
        await NotifyChange();
    }

    /// <inheritdoc/>
    public async Task Rename(ObserverId currentId, ObserverId newId)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var existing = await scope.DbContext.Observers.FindAsync(currentId);
        if (existing is null)
        {
            return;
        }

        var renamed = new ObserverState
        {
            Id = newId,
            LastHandledEventSequenceNumber = existing.LastHandledEventSequenceNumber,
            TailEventSequenceNumber = existing.TailEventSequenceNumber,
            RunningState = existing.RunningState,
            ReplayingPartitions = existing.ReplayingPartitions,
            CatchingUpPartitions = existing.CatchingUpPartitions,
            FailedPartitions = existing.FailedPartitions,
            FailedPartitionCount = existing.FailedPartitionCount,
            IsReplaying = existing.IsReplaying
        };

        scope.DbContext.Observers.Remove(existing);
        await scope.DbContext.Observers.AddAsync(renamed);
        await scope.DbContext.SaveChangesAsync();
        await NotifyChange();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _subject.Dispose();
        GC.SuppressFinalize(this);
    }

    async Task NotifyChange()
    {
        var all = await GetAll();
        _subject.OnNext(all);
    }
}
