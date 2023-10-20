// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Observation;
using Aksio.Cratis.Kernel.Persistence.Observation;
using Aksio.Cratis.Observation;
using Aksio.DependencyInversion;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Observation;

/// <summary>
/// Represents an implementation of <see cref="IFailedPartitionsStorage"/> for MongoDB.
/// </summary>
public class MongoDBFailedPartitionStorage : IFailedPartitionsStorage
{
    readonly ProviderFor<IEventStoreDatabase> _eventStoreDatabaseProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBObserverStorage"/> class.
    /// </summary>
    /// <param name="eventStoreDatabaseProvider">Provider for <see cref="IEventStoreDatabase"/>.</param>
    public MongoDBFailedPartitionStorage(
        ProviderFor<IEventStoreDatabase> eventStoreDatabaseProvider)
    {
        _eventStoreDatabaseProvider = eventStoreDatabaseProvider;
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<FailedPartition>> All
    {
        get
        {
            var observers = Collection.Find(_ => true).ToList();
            return Collection.Observe(observers, (cursor, items) =>
            {
                items.Clear();
                items.AddRange(Collection.Find(_ => true).ToList());
            });
        }
    }

    IMongoCollection<FailedPartition> Collection => _eventStoreDatabaseProvider().GetCollection<FailedPartition>(WellKnownCollectionNames.FailedPartitions);

    /// <inheritdoc/>
    public async Task Save(ObserverId observerId, FailedPartitions failedPartitions)
    {
        foreach (var failedPartition in failedPartitions.Partitions)
        {
            if (failedPartition.IsResolved)
            {
                await Collection.DeleteOneAsync(_ => _.Id == failedPartition.Id).ConfigureAwait(false);
            }
            else
            {
                await Collection.ReplaceOneAsync(
                    _ => _.Id == failedPartition.Id,
                    failedPartition!,
                    new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
            }
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
