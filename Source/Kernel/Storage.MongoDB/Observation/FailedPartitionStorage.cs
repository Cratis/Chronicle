// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation;

/// <summary>
/// Represents an implementation of <see cref="IFailedPartitionsStorage"/> for MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ObserverStorage"/> class.
/// </remarks>
/// <param name="database">Provider for <see cref="IEventStoreNamespaceDatabase"/>.</param>
public class FailedPartitionStorage(IEventStoreNamespaceDatabase database) : IFailedPartitionsStorage
{
    IMongoCollection<FailedPartition> Collection => database.GetCollection<FailedPartition>(WellKnownCollectionNames.FailedPartitions);

    /// <inheritdoc/>
    public IObservable<IEnumerable<FailedPartition>> ObserveAllFor(ObserverId? observerId = default)
    {
        throw new NotImplementedException();
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
