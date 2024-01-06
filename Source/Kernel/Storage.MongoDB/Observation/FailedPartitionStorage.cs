// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Observation;
using Aksio.Cratis.Kernel.Storage.Observation;
using Aksio.Cratis.Observation;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.Storage.MongoDB.Observation;

/// <summary>
/// Represents an implementation of <see cref="IFailedPartitionsStorage"/> for MongoDB.
/// </summary>
public class FailedPartitionStorage : IFailedPartitionsStorage
{
    readonly IEventStoreNamespaceDatabase _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverStorage"/> class.
    /// </summary>
    /// <param name="database">Provider for <see cref="IEventStoreNamespaceDatabase"/>.</param>
    public FailedPartitionStorage(IEventStoreNamespaceDatabase database)
    {
        _database = database;
    }

    IMongoCollection<FailedPartition> Collection => _database.GetCollection<FailedPartition>(WellKnownCollectionNames.FailedPartitions);

    /// <inheritdoc/>
    public IObservable<IEnumerable<FailedPartition>> ObserveAllFor(ObserverId? observerId = default)
    {
        var filter = observerId == default || observerId == ObserverId.Unspecified ?
            Builders<FailedPartition>.Filter.Empty :
            Builders<FailedPartition>.Filter.Eq(_ => _.ObserverId, observerId);

        var observers = Collection.Find(filter).ToList();
        return Collection.Observe(observers, (cursor, items) =>
        {
            items.Clear();
            items.AddRange(Collection.Find(filter).ToList());
        });
    }

    /// <inheritdoc/>
    public async Task Save(ObserverId observerId, FailedPartitions failedPartitions)
    {
        foreach (var failedPartition in failedPartitions.ResolvedPartitions)
        {
            await Collection.DeleteOneAsync(_ => _.Id == failedPartition.Id).ConfigureAwait(false);
        }

        foreach (var failedPartition in failedPartitions.Partitions)
        {
            await Collection.ReplaceOneAsync(
                _ => _.Id == failedPartition.Id,
                failedPartition!,
                new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task<FailedPartitions> GetFor(ObserverId observerId)
    {
        var cursor = await Collection.FindAsync(_ => _.ObserverId == observerId).ConfigureAwait(false);

        return new FailedPartitions
        {
            Partitions = cursor.ToList()
        };
    }
}
