// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
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
    readonly IMongoCollection<FailedPartition> _collection = database.GetCollection<FailedPartition>(WellKnownCollectionNames.FailedPartitions);

    /// <inheritdoc/>
    public ISubject<IEnumerable<FailedPartition>> ObserveAllFor(ObserverId? observerId = default)
    {
        if (observerId == default) return _collection.Observe();
        return _collection.Observe(_ => _.ObserverId == observerId);
    }

    /// <inheritdoc/>
    public async Task Save(ObserverId observerId, FailedPartitions failedPartitions)
    {
        foreach (var failedPartition in failedPartitions.ResolvedPartitions)
        {
            await _collection.DeleteOneAsync(_ => _.Id == failedPartition.Id).ConfigureAwait(false);
        }

        foreach (var failedPartition in failedPartitions.Partitions)
        {
            await _collection.ReplaceOneAsync(
                _ => _.Id == failedPartition.Id,
                failedPartition!,
                new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task<FailedPartitions> GetFor(ObserverId? observerId)
    {
        using var cursor = observerId is null ?
            await _collection.FindAsync(_ => true).ConfigureAwait(false) :
            await _collection.FindAsync(_ => _.ObserverId == observerId).ConfigureAwait(false);

        return new FailedPartitions
        {
            Partitions = cursor.ToList()
        };
    }
}
