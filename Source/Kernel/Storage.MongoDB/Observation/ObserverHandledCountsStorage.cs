// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObserverHandledCountsStorage"/> for MongoDB.
/// </summary>
/// <param name="database">The <see cref="IEventStoreNamespaceDatabase"/> to use for accessing the collection.</param>
public class ObserverHandledCountsStorage(IEventStoreNamespaceDatabase database) : IObserverHandledCountsStorage
{
    readonly IMongoCollection<ObserverPartitionCounts> _collection = database.GetCollection<ObserverPartitionCounts>(WellKnownCollectionNames.ObserverHandledCounts);

    /// <inheritdoc/>
    public async Task Increment(ObserverId observerId, Key partition, IReadOnlyDictionary<EventTypeId, EventCount> countsPerEventType)
    {
        if (countsPerEventType.Count == 0)
        {
            return;
        }

        var id = new ObserverPartitionCountsId(observerId, partition.ToString());
        var increments = countsPerEventType.Select(_ =>
            Builders<ObserverPartitionCounts>.Update.Inc(
                new StringFieldDefinition<ObserverPartitionCounts, long>($"{nameof(ObserverPartitionCounts.Counts)}.{_.Key.Value}"),
                (long)_.Value.Value));
        var update = Builders<ObserverPartitionCounts>.Update.Combine(increments);
        var model = new UpdateOneModel<ObserverPartitionCounts>(
            Builders<ObserverPartitionCounts>.Filter.Eq(_ => _.Id, id),
            update)
        {
            IsUpsert = true
        };

        await _collection.BulkWriteAsync([model]).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<EventTypeId, EventCount>> GetFor(ObserverId observerId, Key partition)
    {
        var id = new ObserverPartitionCountsId(observerId, partition.ToString());
        var document = await _collection.Find(_ => _.Id == id).FirstOrDefaultAsync().ConfigureAwait(false);
        if (document is null)
        {
            return ImmutableDictionary<EventTypeId, EventCount>.Empty;
        }

        return document.Counts.ToDictionary(
            _ => (EventTypeId)_.Key,
            _ => (EventCount)_.Value);
    }

    /// <inheritdoc/>
    public async Task RemoveFor(ObserverId observerId, Key partition)
    {
        var id = new ObserverPartitionCountsId(observerId, partition.ToString());
        await _collection.DeleteOneAsync(_ => _.Id == id).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RemoveAllFor(ObserverId observerId) =>
        await _collection.DeleteManyAsync(_ => _.Id.ObserverId == observerId).ConfigureAwait(false);
}
