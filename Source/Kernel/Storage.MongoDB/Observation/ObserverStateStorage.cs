// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Reactive;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObserverStateStorage"/> for MongoDB.
/// </summary>
/// <param name="namespaceDatabase"><see cref="IEventStoreNamespaceDatabase"/>.</param>
public class ObserverStateStorage(IEventStoreNamespaceDatabase namespaceDatabase) : IObserverStateStorage
{
    IMongoCollection<ObserverState> _collection => namespaceDatabase.GetObserverStateCollection();

    IMongoCollection<ObserverPartitionCounts> _handledCountsCollection => namespaceDatabase.GetCollection<ObserverPartitionCounts>(WellKnownCollectionNames.ObserverHandledCounts);

    /// <inheritdoc/>
    public ISubject<IEnumerable<Chronicle.Storage.Observation.ObserverState>> ObserveAll()
    {
        var collectionSubject = _collection.Observe();
        return new TransformingSubject<IEnumerable<ObserverState>, IEnumerable<Chronicle.Storage.Observation.ObserverState>>(
            collectionSubject,
            observers => observers.ToKernel());
    }

    /// <inheritdoc/>
    public async Task<Chronicle.Storage.Observation.ObserverState> Get(ObserverId observerId)
    {
        var state = await _collection
            .Aggregate()
            .Match(_ => _.Id == observerId)
            .JoinWithFailedPartitions()
            .FirstOrDefaultAsync().ConfigureAwait(false);

        if (state is null)
        {
            return Chronicle.Storage.Observation.ObserverState.Empty;
        }

        if (state.HandledEventCountPerPartition.Count > 0)
        {
            await SplitLegacyPerPartitionCounts(observerId, state.HandledEventCountPerPartition).ConfigureAwait(false);
        }

        return state.ToKernel();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Chronicle.Storage.Observation.ObserverState>> GetAll()
    {
        var aggregation = _collection.Aggregate().JoinWithFailedPartitions();
        var cursor = await aggregation.ToCursorAsync();
        return (await cursor.ToListAsync()).ToKernel();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Writes a targeted <c>$set</c> of the observer-owned fields rather than replacing the whole document, so
    /// fields the observer state does not own — such as legacy per-partition counts that are still awaiting
    /// migration on the next load — are left untouched instead of being wiped by a full replace.
    /// </remarks>
    public async Task Save(Chronicle.Storage.Observation.ObserverState state)
    {
        var document = state.ToMongoDB();
        var update = Builders<ObserverState>.Update
            .Set(_ => _.LastHandledEventSequenceNumber, document.LastHandledEventSequenceNumber)
            .Set(_ => _.NextEventSequenceNumber, document.NextEventSequenceNumber)
            .Set(_ => _.TailEventSequenceNumber, document.TailEventSequenceNumber)
            .Set(_ => _.RunningState, document.RunningState)
            .Set(_ => _.ReplayingPartitions, document.ReplayingPartitions)
            .Set(_ => _.CatchingUpPartitions, document.CatchingUpPartitions)
            .Set(_ => _.InFlightPartitions, document.InFlightPartitions)
            .Set(_ => _.FailedPartitionCount, document.FailedPartitionCount)
            .Set(_ => _.IsReplaying, document.IsReplaying)
            .Set(_ => _.SubscribesToAllEvents, document.SubscribesToAllEvents)
            .Set(_ => _.HandledEventCount, document.HandledEventCount)
            .Set(_ => _.HandledEventCountPerEventType, document.HandledEventCountPerEventType);

        await _collection.UpdateOneAsync(
            os => os.Id == state.Identifier,
            update,
            new UpdateOptions { IsUpsert = true }).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task Rename(ObserverId currentId, ObserverId newId)
    {
        var update = Builders<ObserverState>.Update.Set(os => os.Id, newId);
        await _collection.UpdateOneAsync(
            os => os.Id == currentId,
            update);
    }

    async Task SplitLegacyPerPartitionCounts(ObserverId observerId, IDictionary<string, IDictionary<string, ulong>> legacyCounts)
    {
        var writes = legacyCounts.Select(partitionEntry =>
        {
            var id = new ObserverPartitionCountsId(observerId, partitionEntry.Key);
            var document = new ObserverPartitionCounts
            {
                Id = id,
                Counts = partitionEntry.Value.ToDictionary(_ => _.Key, _ => (long)_.Value)
            };

            return new ReplaceOneModel<ObserverPartitionCounts>(
                Builders<ObserverPartitionCounts>.Filter.Eq(_ => _.Id, id),
                document)
            {
                IsUpsert = true
            };
        }).ToArray();

        if (writes.Length > 0)
        {
            await _handledCountsCollection.BulkWriteAsync(writes).ConfigureAwait(false);
        }

        await _collection.UpdateOneAsync(
            _ => _.Id == observerId,
            Builders<ObserverState>.Update.Unset(_ => _.HandledEventCountPerPartition)).ConfigureAwait(false);
    }
}
