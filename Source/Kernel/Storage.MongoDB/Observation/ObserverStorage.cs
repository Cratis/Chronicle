// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Reactive;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObserverStateStorage"/> for MongoDB.
/// </summary>
/// <param name="namespaceDatabase"><see cref="IEventStoreNamespaceDatabase"/>.</param>
public class ObserverStorage(IEventStoreNamespaceDatabase namespaceDatabase) : IObserverStateStorage
{
    IMongoCollection<ObserverState> _collection => namespaceDatabase.GetObserverStateCollection();

    /// <inheritdoc/>
    public ISubject<IEnumerable<Chronicle.Storage.Observation.ObserverState>> ObserveAll()
    {
        var collectionSubject = _collection.Observe();
        return new TransformingSubject<IEnumerable<ObserverState>, IEnumerable<ObserverInformation>>(
            collectionSubject,
            observers => observers.Select(_ => ToObserverInformation(_)).ToArray());
    }

    /// <inheritdoc/>
    public Task<Chronicle.Storage.Observation.ObserverState> Get(ObserverId observerId) =>
        _collection
            .Aggregate()
            .Match(_ => _.Id == observerId)
            .JoinWithFailedPartitions()
            .FirstAsync();

    /// <inheritdoc/>
    public async Task<IEnumerable<Chronicle.Storage.Observation.ObserverState>> GetAll()
    {
        var aggregation = _collection.Aggregate().JoinWithFailedPartitions();
        var cursor = await aggregation.ToCursorAsync();
        return cursor.ToList();
    }

    /// <inheritdoc/>
    public async Task Save(Chronicle.Storage.Observation.ObserverState state)
    {
        await _collection.ReplaceOneAsync(
            os => os.Id == state.Id,
            state!,
            new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
    }
}
