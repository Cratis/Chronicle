// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions;

/// <summary>
/// Represents an implementation of <see cref="IFailedPartitionsStorage"/> using SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="namespace">The name of the namespace.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class FailedPartitionStorage(
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    IDatabase database) : IFailedPartitionsStorage
{
    /// <inheritdoc/>
    public ISubject<IEnumerable<Concepts.Observation.FailedPartition>> ObserveAllFor(ObserverId? observerId = default)
    {
        // For SQL implementation, we'll create a simple subject that provides current data
        // This could be enhanced with actual database change tracking in the future
        var subject = new BehaviorSubject<IEnumerable<Concepts.Observation.FailedPartition>>([]);

        Task.Run(async () =>
        {
            var failedPartitions = await GetFor(observerId);
            subject.OnNext(failedPartitions.Partitions);
        });

        return subject;
    }

    /// <inheritdoc/>
    public async Task Save(ObserverId observerId, Concepts.Observation.FailedPartitions failedPartitions)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);

        // Remove resolved partitions
        foreach (var resolvedPartition in failedPartitions.ResolvedPartitions)
        {
            var entity = await scope.DbContext.FailedPartitions.FirstOrDefaultAsync(fp => fp.Id == resolvedPartition.Id.Value);
            if (entity is not null)
            {
                scope.DbContext.FailedPartitions.Remove(entity);
            }
        }

        // Save or update failed partitions
        foreach (var failedPartition in failedPartitions.Partitions)
        {
            var entity = failedPartition.ToEntity();
            await scope.DbContext.FailedPartitions.Upsert(entity);
        }

        await scope.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<Concepts.Observation.FailedPartitions> GetFor(ObserverId? observerId)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);

        var query = scope.DbContext.FailedPartitions.AsQueryable();
        if (observerId is not null)
        {
            query = query.Where(fp => fp.ObserverId == observerId);
        }

        var entities = await query.ToListAsync();
        var failedPartitions = entities.Select(e => e.ToFailedPartition());

        return new Concepts.Observation.FailedPartitions
        {
            Partitions = failedPartitions
        };
    }
}
